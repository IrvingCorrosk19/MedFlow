/**
 * MedFlow Shell Experience — tema oscuro + paleta de comandos (Ctrl/Cmd+K)
 */
(function () {
    var THEME_KEY = 'mf-theme';

    function applyTheme(theme) {
        var t = theme === 'dark' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-mf-theme', t);
        try {
            localStorage.setItem(THEME_KEY, t);
        } catch (e) { /* ignore */ }
        var btn = document.getElementById('mfThemeToggle');
        if (btn) {
            btn.setAttribute('aria-pressed', t === 'dark' ? 'true' : 'false');
            btn.title = t === 'dark' ? 'Modo claro' : 'Modo oscuro';
            var icon = btn.querySelector('i');
            if (icon) {
                icon.className = t === 'dark' ? 'fa fa-sun-o' : 'fa fa-moon-o';
            }
        }
    }

    function initTheme() {
        var saved = null;
        try {
            saved = localStorage.getItem(THEME_KEY);
        } catch (e) { /* ignore */ }
        if (saved === 'dark' || saved === 'light') {
            applyTheme(saved);
            return;
        }
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            applyTheme('dark');
        } else {
            applyTheme('light');
        }
    }

    function toggleTheme() {
        var cur = document.documentElement.getAttribute('data-mf-theme') || 'light';
        applyTheme(cur === 'dark' ? 'light' : 'dark');
    }

    var palette, input, items;

    function visibleItems() {
        return Array.prototype.filter.call(items, function (b) {
            return b.style.display !== 'none';
        });
    }

    function setActiveIndex(idx) {
        var v = visibleItems();
        if (!v.length) return;
        var i = Math.max(0, Math.min(idx, v.length - 1));
        v.forEach(function (b) { b.classList.remove('mf-xp-cmd-active'); });
        v[i].classList.add('mf-xp-cmd-active');
        v[i].scrollIntoView({ block: 'nearest' });
    }

    function openPalette() {
        if (!palette) return;
        palette.classList.add('mf-xp-cmd-open');
        palette.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
        if (input) {
            input.value = '';
            filter('');
            input.focus();
        }
    }

    function closePalette() {
        if (!palette) return;
        palette.classList.remove('mf-xp-cmd-open');
        palette.setAttribute('aria-hidden', 'true');
        document.body.style.overflow = '';
    }

    function filter(q) {
        q = (q || '').trim().toLowerCase();
        Array.prototype.forEach.call(items, function (btn) {
            var hay = (btn.getAttribute('data-mf-search') || btn.textContent || '').toLowerCase();
            btn.style.display = !q || hay.indexOf(q) >= 0 ? '' : 'none';
        });
        setActiveIndex(0);
    }

    function moveActive(delta) {
        var v = visibleItems();
        if (!v.length) return;
        var cur = v.findIndex(function (b) { return b.classList.contains('mf-xp-cmd-active'); });
        if (cur < 0) cur = 0;
        setActiveIndex(cur + delta);
    }

    function activateCurrent() {
        var el = document.querySelector('#mfCmdPalette .mf-xp-cmd-item.mf-xp-cmd-active');
        if (el && el.style.display !== 'none') {
            var href = el.getAttribute('data-href');
            if (href) window.location.href = href;
        }
    }

    function initPalette() {
        palette = document.getElementById('mfCmdPalette');
        if (!palette) return;
        input = palette.querySelector('.mf-xp-cmd-input');
        items = palette.querySelectorAll('.mf-xp-cmd-item');

        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                if (palette.classList.contains('mf-xp-cmd-open')) closePalette();
                else openPalette();
            }
            if (e.key === 'Escape' && palette.classList.contains('mf-xp-cmd-open')) {
                e.preventDefault();
                closePalette();
            }
        });

        palette.addEventListener('click', function (e) {
            if (e.target === palette) closePalette();
        });

        if (input) {
            input.addEventListener('input', function () {
                filter(input.value);
            });
            input.addEventListener('keydown', function (e) {
                if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    moveActive(1);
                } else if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    moveActive(-1);
                } else if (e.key === 'Enter') {
                    e.preventDefault();
                    activateCurrent();
                }
            });
        }

        Array.prototype.forEach.call(items, function (btn) {
            btn.addEventListener('click', function () {
                var href = btn.getAttribute('data-href');
                if (href) window.location.href = href;
            });
        });

        var openBtn = document.getElementById('mfCmdPaletteBtn');
        if (openBtn) openBtn.addEventListener('click', openPalette);
    }

    document.addEventListener('DOMContentLoaded', function () {
        initTheme();
        var themeBtn = document.getElementById('mfThemeToggle');
        if (themeBtn) themeBtn.addEventListener('click', toggleTheme);
        initPalette();
    });
})();
