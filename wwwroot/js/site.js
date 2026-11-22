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
            var lightLabel = el.getAttribute('data-light-label') || 'Light mode';
            var darkLabel = el.getAttribute('data-dark-label') || 'Dark mode';
            el.textContent = dark ? lightLabel : darkLabel;
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

// ── Relative dates ──
document.addEventListener('DOMContentLoaded', function () {
    if (!window.Intl || !Intl.RelativeTimeFormat) return;
    var rtf = new Intl.RelativeTimeFormat(document.documentElement.lang || undefined, { numeric: 'auto' });
    var units = [['year', 31536000], ['month', 2592000], ['day', 86400], ['hour', 3600], ['minute', 60]];
    document.querySelectorAll('time[data-relative-time]').forEach(function (el) {
        var date = new Date(el.getAttribute('datetime'));
        if (isNaN(date)) return;
        el.title = el.textContent;
        var seconds = Math.round((date - new Date()) / 1000);
        for (var i = 0; i < units.length; i++) {
            if (Math.abs(seconds) >= units[i][1]) {
                el.textContent = rtf.format(Math.round(seconds / units[i][1]), units[i][0]);
                return;
            }
        }
        el.textContent = rtf.format(0, 'second');
    });
});

// ── List search / type filter / delete modal ──
document.addEventListener('DOMContentLoaded', function () {
    var search = document.getElementById('listSearch');
    var pills = document.querySelectorAll('[data-list-filter]');
    var groups = document.querySelectorAll('.list-group-section');
    var noMatches = document.getElementById('listNoMatches');
    var activeType = 'all';

    function applyFilters() {
        var term = search ? search.value.trim().toLowerCase() : '';
        var anyVisible = false;
        document.querySelectorAll('.list-card-col').forEach(function (col) {
            var matches = (activeType === 'all' || col.getAttribute('data-list-type') === activeType) &&
                (!term || (col.getAttribute('data-list-name') || '').toLowerCase().indexOf(term) !== -1);
            col.classList.toggle('d-none', !matches);
            anyVisible = anyVisible || matches;
        });
        var filtering = term || activeType !== 'all';
        groups.forEach(function (group) {
            var hasVisible = group.querySelector('.list-card-col:not(.d-none)');
            group.classList.toggle('d-none', !!filtering && !hasVisible);
        });
        if (noMatches) noMatches.classList.toggle('d-none', anyVisible || !document.querySelector('.list-card-col'));
    }

    if (search) search.addEventListener('input', applyFilters);
    pills.forEach(function (pill) {
        pill.addEventListener('click', function () {
            activeType = pill.getAttribute('data-list-filter');
            pills.forEach(function (p) { p.classList.toggle('active', p === pill); });
            applyFilters();
        });
    });

    var modalEl = document.getElementById('deleteListModal');
    if (modalEl) {
        var modal = new bootstrap.Modal(modalEl);
        document.querySelectorAll('.delete-list-btn').forEach(function (button) {
            button.addEventListener('click', function () {
                document.getElementById('deleteListName').textContent = button.getAttribute('data-list-name') || '';
                document.getElementById('deleteListId').value = button.getAttribute('data-list-id');
                modal.show();
            });
        });
    }
});
