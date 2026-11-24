// Reads text from recipe screenshots in the browser (Tesseract.js) and fills the matching
// text box. Screenshots can be pasted (Ctrl+V / long-press Paste), chosen as a file,
// dropped onto a paste area, or read from the clipboard with the "Paste screenshot" button.
(function () {
    'use strict';

    var root = document.querySelector('[data-recipe-ocr]');
    if (!root)
        return;

    var messages = JSON.parse(root.getAttribute('data-messages') || '{}');
    var sections = Array.prototype.slice.call(root.querySelectorAll('[data-ocr-section]'));
    if (sections.length === 0)
        return;

    var activeSection = sections[0];
    var workerPromise = null;
    var progressHandler = null;
    var queue = Promise.resolve();

    function textAreaFor(section) { return document.getElementById(section.getAttribute('data-ocr-section')); }
    function statusFor(section) { return section.querySelector('[data-ocr-status]'); }

    function setStatus(section, text, isError) {
        var status = statusFor(section);
        status.textContent = text;
        status.classList.toggle('text-danger', !!isError);
        status.classList.toggle('text-muted', !isError);
    }

    function sectionOf(element) {
        for (var i = 0; i < sections.length; i++)
            if (sections[i].contains(element))
                return sections[i];
        return null;
    }

    function getWorker() {
        if (!workerPromise) {
            workerPromise = Tesseract.createWorker('eng', 1, {
                logger: function (m) { if (progressHandler) progressHandler(m); }
            });
            workerPromise.catch(function () { workerPromise = null; });
        }
        return workerPromise;
    }

    // Screenshots are often small, low-contrast or dark mode. Scale up, convert to
    // grayscale and invert light-on-dark images so Tesseract sees dark text on white.
    function prepareImage(file) {
        return createImageBitmap(file).then(function (bitmap) {
            var scale = Math.min(3, Math.max(1, 1600 / bitmap.width));
            var maxSide = 5000;
            scale = Math.min(scale, maxSide / bitmap.width, maxSide / bitmap.height);
            var canvas = document.createElement('canvas');
            canvas.width = Math.max(1, Math.round(bitmap.width * scale));
            canvas.height = Math.max(1, Math.round(bitmap.height * scale));
            var ctx = canvas.getContext('2d');
            ctx.fillStyle = '#fff';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.imageSmoothingQuality = 'high';
            ctx.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
            if (bitmap.close) bitmap.close();

            var image = ctx.getImageData(0, 0, canvas.width, canvas.height);
            var data = image.data, total = 0, i;
            for (i = 0; i < data.length; i += 4) {
                var gray = 0.299 * data[i] + 0.587 * data[i + 1] + 0.114 * data[i + 2];
                data[i] = data[i + 1] = data[i + 2] = gray;
                total += gray;
            }
            if (total / (data.length / 4) < 110) {
                for (i = 0; i < data.length; i += 4)
                    data[i] = data[i + 1] = data[i + 2] = 255 - data[i];
            }
            ctx.putImageData(image, 0, 0);
            return canvas;
        }).catch(function () {
            return file; // Let Tesseract try the original image.
        });
    }

    // OCR turns bullets and checkboxes into stray symbols or single letters ("«", "»", "J 1 cup").
    var bulletPattern = /^(?:[^A-Za-z0-9\u00BC-\u00BE\u2150-\u215E\s]{1,3}|[B-HJ-Zb-z](?=\s+[\d\u00BC-\u00BE\u2150-\u215E]))\s+/;

    function cleanIngredients(text) {
        return text.split(/\r?\n/)
            .map(function (line) { return line.replace(/\s+/g, ' ').trim().replace(bulletPattern, '').trim(); })
            .filter(function (line) { return /[A-Za-z0-9\u00BC-\u00BE\u2150-\u215E]/.test(line); })
            .join('\n');
    }

    // Tesseract keeps the screenshot's line wrapping. Re-join wrapped lines into one line
    // per step; a blank line or a new number/bullet starts a new step.
    function cleanInstructions(text) {
        var steps = [], current = '';
        text.split(/\r?\n/).forEach(function (raw) {
            var line = raw.replace(/\s+/g, ' ').trim();
            var startsStep = /^(\d{1,2}\s*[.):]|step\s*\d+|[•\-*])/i.test(line);
            if (!line || startsStep) {
                if (current) steps.push(current);
                current = line;
            } else {
                current = current ? current + ' ' + line : line;
            }
        });
        if (current) steps.push(current);
        return steps.join('\n');
    }

    function recognize(file, section) {
        if (!file || !/^image\//.test(file.type || 'image/')) {
            setStatus(section, messages.notImage, true);
            return Promise.resolve();
        }
        if (typeof Tesseract === 'undefined') {
            setStatus(section, messages.unavailable, true);
            return Promise.resolve();
        }

        var textArea = textAreaFor(section);
        setStatus(section, messages.reading);
        progressHandler = function (m) {
            if (m && m.status === 'recognizing text' && typeof m.progress === 'number')
                setStatus(section, messages.reading + ' ' + Math.round(m.progress * 100) + '%');
        };

        return Promise.all([getWorker(), prepareImage(file)])
            .then(function (results) { return results[0].recognize(results[1]); })
            .then(function (result) {
                var raw = (result && result.data && result.data.text || '').trim();
                var text = textArea.id === 'Instructions' ? cleanInstructions(raw) : cleanIngredients(raw);
                if (!text) {
                    setStatus(section, messages.noText, true);
                    return;
                }
                var existing = textArea.value.replace(/\s+$/, '');
                textArea.value = existing ? existing + '\n' + text : text;
                textArea.dispatchEvent(new Event('input', { bubbles: true }));
                setStatus(section, messages.done);
            })
            .catch(function (e) {
                console.error('Screenshot text recognition failed', e);
                setStatus(section, messages.failed, true);
            })
            .then(function () { progressHandler = null; });
    }

    // Run one image at a time so progress messages go to the right section.
    function enqueue(file, section) {
        queue = queue.then(function () { return recognize(file, section); });
        return queue;
    }

    function imageFromDataTransfer(dataTransfer) {
        if (!dataTransfer)
            return null;
        var files = Array.prototype.slice.call(dataTransfer.files || []);
        var file = files.filter(function (f) { return /^image\//.test(f.type); })[0];
        if (file)
            return file;
        var items = Array.prototype.slice.call(dataTransfer.items || []);
        var item = items.filter(function (i) { return i.kind === 'file' && /^image\//.test(i.type); })[0];
        return item ? item.getAsFile() : null;
    }

    sections.forEach(function (section) {
        section.addEventListener('focusin', function () { activeSection = section; });
        section.addEventListener('pointerdown', function () { activeSection = section; });
        section.addEventListener('click', function () { activeSection = section; });

        var fileInput = section.querySelector('[data-ocr-file]');
        if (fileInput) {
            fileInput.addEventListener('change', function () {
                if (fileInput.files && fileInput.files[0])
                    enqueue(fileInput.files[0], section);
                fileInput.value = '';
            });
        }

        var zone = section.querySelector('[data-ocr-zone]');
        if (zone) {
            ['dragenter', 'dragover'].forEach(function (type) {
                zone.addEventListener(type, function (e) { e.preventDefault(); zone.classList.add('border-primary'); });
            });
            ['dragleave', 'drop'].forEach(function (type) {
                zone.addEventListener(type, function () { zone.classList.remove('border-primary'); });
            });
            zone.addEventListener('drop', function (e) {
                e.preventDefault();
                var file = imageFromDataTransfer(e.dataTransfer);
                if (file) enqueue(file, section);
            });
        }

        var pasteButton = section.querySelector('[data-ocr-paste]');
        if (pasteButton) {
            if (!navigator.clipboard || !navigator.clipboard.read) {
                (pasteButton.closest('li') || pasteButton).hidden = true;
            } else {
                pasteButton.addEventListener('click', function () {
                    activeSection = section;
                    navigator.clipboard.read().then(function (clipboardItems) {
                        for (var i = 0; i < clipboardItems.length; i++) {
                            var type = clipboardItems[i].types.filter(function (t) { return /^image\//.test(t); })[0];
                            if (type) {
                                return clipboardItems[i].getType(type).then(function (blob) {
                                    return enqueue(new File([blob], 'screenshot', { type: blob.type || type }), section);
                                });
                            }
                        }
                        setStatus(section, messages.noImage, true);
                    }).catch(function () {
                        setStatus(section, messages.clipboardDenied, true);
                    });
                });
            }
        }
    });

    // Ctrl+V anywhere on the page: send a pasted image to the section the user is in
    // (or last used). Pasted text is left alone so normal text pasting still works.
    document.addEventListener('paste', function (e) {
        var file = imageFromDataTransfer(e.clipboardData);
        if (!file)
            return;
        e.preventDefault();
        enqueue(file, sectionOf(e.target) || sectionOf(document.activeElement) || activeSection);
    });
})();
