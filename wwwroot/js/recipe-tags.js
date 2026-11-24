// Tag entry on the recipe form: Enter or a comma turns the typed text into a tag chip
// (instead of submitting the form). The chips are kept in a hidden comma-separated input.
(function () {
    'use strict';

    var maxTags = 30;

    document.querySelectorAll('[data-tag-input]').forEach(function (root) {
        var valueInput = root.querySelector('[data-tag-value]');
        var entry = root.querySelector('[data-tag-entry]');
        var chips = root.querySelector('[data-tag-chips]');
        var removeLabel = root.getAttribute('data-remove-label') || 'Remove';
        var tags = [];

        function clean(text) {
            return text.trim().replace(/^#+/, '').trim().slice(0, 50).trim();
        }

        function has(name) {
            var lower = name.toLowerCase();
            return tags.some(function (t) { return t.toLowerCase() === lower; });
        }

        function render() {
            chips.textContent = '';
            tags.forEach(function (name, index) {
                var chip = document.createElement('span');
                chip.className = 'badge rounded-pill badge-theme-neutral d-inline-flex align-items-center gap-1 recipe-tag';
                chip.appendChild(document.createTextNode(name));
                var remove = document.createElement('button');
                remove.type = 'button';
                remove.className = 'btn-close recipe-tag-remove';
                remove.setAttribute('aria-label', removeLabel + ' ' + name);
                remove.title = removeLabel;
                remove.addEventListener('click', function () {
                    tags.splice(index, 1);
                    render();
                    entry.focus();
                });
                chip.appendChild(remove);
                chips.appendChild(chip);
            });
            valueInput.value = tags.join(', ');
        }

        // Adds every comma-separated name in text; returns true if anything was typed.
        function addFrom(text) {
            var any = false;
            text.split(',').forEach(function (part) {
                var name = clean(part);
                if (!name)
                    return;
                any = true;
                if (tags.length < maxTags && !has(name))
                    tags.push(name);
            });
            if (any)
                render();
            return any;
        }

        function commitEntry() {
            if (addFrom(entry.value))
                entry.value = '';
        }

        entry.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.isComposing) {
                e.preventDefault();
                commitEntry();
            } else if (e.key === 'Backspace' && entry.value === '' && tags.length > 0) {
                tags.pop();
                render();
            }
        });

        // Typing or pasting a comma finishes the tag before it.
        entry.addEventListener('input', function () {
            var value = entry.value;
            var lastComma = value.lastIndexOf(',');
            if (lastComma < 0)
                return;
            addFrom(value.slice(0, lastComma));
            entry.value = value.slice(lastComma + 1).replace(/^\s+/, '');
        });

        entry.addEventListener('blur', commitEntry);

        // Don't lose a tag that was typed but not yet added.
        var form = root.closest('form');
        if (form)
            form.addEventListener('submit', commitEntry);

        // Restore tags after a failed submit re-renders the form.
        addFrom(valueInput.value);
        render();
    });
})();
