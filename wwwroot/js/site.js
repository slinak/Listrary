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

// ── Index pages: search box and filter pills ──
// Cards are .entity-col elements with data-filter-name (searched text) and
// data-filter-tags ("|"-separated values the pills match against).
document.addEventListener('DOMContentLoaded', function () {
    var search = document.getElementById('entitySearch');
    var pills = document.querySelectorAll('[data-entity-filter]');
    var groups = document.querySelectorAll('.list-group-section');
    var noMatches = document.getElementById('entityNoMatches');
    var activeFilter = 'all';

    function applyFilters() {
        var term = search ? search.value.trim().toLowerCase() : '';
        var anyVisible = false;
        document.querySelectorAll('.entity-col').forEach(function (col) {
            var tags = (col.getAttribute('data-filter-tags') || '').toLowerCase().split('|');
            var matches = (activeFilter === 'all' || tags.indexOf(activeFilter) !== -1) &&
                (!term || (col.getAttribute('data-filter-name') || '').toLowerCase().indexOf(term) !== -1);
            col.classList.toggle('d-none', !matches);
            anyVisible = anyVisible || matches;
        });
        var filtering = term || activeFilter !== 'all';
        groups.forEach(function (group) {
            var hasVisible = group.querySelector('.entity-col:not(.d-none)');
            group.classList.toggle('d-none', !!filtering && !hasVisible);
        });
        if (noMatches) noMatches.classList.toggle('d-none', anyVisible || !document.querySelector('.entity-col'));
    }

    if (search) search.addEventListener('input', applyFilters);
    pills.forEach(function (pill) {
        pill.addEventListener('click', function () {
            activeFilter = (pill.getAttribute('data-entity-filter') || 'all').toLowerCase();
            pills.forEach(function (p) {
                p.classList.toggle('active', p === pill);
                p.setAttribute('aria-pressed', p === pill ? 'true' : 'false');
            });
            applyFilters();
        });
    });

    // A pill can start selected, e.g. a recipe tag opened from a link.
    var preselected = document.querySelector('[data-entity-filter].active');
    if (preselected) {
        activeFilter = (preselected.getAttribute('data-entity-filter') || 'all').toLowerCase();
        applyFilters();
    }
});

// ── Shared delete confirmation for lists, circles and recipes ──
document.addEventListener('DOMContentLoaded', function () {
    var modalEl = document.getElementById('confirmDeleteModal');
    if (!modalEl) return;
    var modal = new bootstrap.Modal(modalEl);
    var form = document.getElementById('confirmDeleteForm');
    document.querySelectorAll('.confirm-delete-btn').forEach(function (button) {
        button.addEventListener('click', function () {
            form.setAttribute('action', button.getAttribute('data-delete-url'));
            document.getElementById('confirmDeletePrompt').textContent = button.getAttribute('data-delete-prompt') || '';
            document.getElementById('confirmDeleteName').textContent = button.getAttribute('data-delete-name') || '';
            modal.show();
        });
    });
});

// ── Recipe page: tick off ingredients while cooking, and the phone ingredients/steps switch ──
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-ingredient-toggle]').forEach(function (button) {
        button.addEventListener('click', function () {
            var row = button.closest('.item-row');
            var checked = !button.classList.contains('checked');
            button.classList.toggle('checked', checked);
            button.setAttribute('aria-pressed', checked ? 'true' : 'false');
            if (row) row.classList.toggle('item-row-completed', checked);
        });
    });

    var recipe = document.querySelector('[data-recipe-view]');
    document.querySelectorAll('[data-recipe-show]').forEach(function (button) {
        button.addEventListener('click', function () {
            recipe.setAttribute('data-recipe-view', button.getAttribute('data-recipe-show'));
            document.querySelectorAll('[data-recipe-show]').forEach(function (b) {
                b.classList.toggle('active', b === button);
                b.setAttribute('aria-pressed', b === button ? 'true' : 'false');
            });
        });
    });
});
