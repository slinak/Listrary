// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ── Dark mode toggle ──
(function () {
    var STORAGE_KEY = 'listrary-theme';

    function isDark() {
        return document.documentElement.getAttribute('data-bs-theme') === 'dark';
    }

    function applyTheme(dark) {
        if (dark) {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            localStorage.setItem(STORAGE_KEY, 'dark');
        } else {
            document.documentElement.removeAttribute('data-bs-theme');
            localStorage.setItem(STORAGE_KEY, 'light');
        }
        updateLabels();
    }

    function updateLabels() {
        var dark = isDark();
        document.querySelectorAll('.dark-mode-label').forEach(function (el) {
            el.textContent = dark ? '☀ Light mode' : '🌙 Dark mode';
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        updateLabels();

        document.querySelectorAll('.dark-mode-toggle').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                applyTheme(!isDark());
            });
        });
    });
}());
