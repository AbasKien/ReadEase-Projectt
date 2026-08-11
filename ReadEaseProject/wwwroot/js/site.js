/* ════════════════════════════════════════════════════════════════
   ReadEase — site.js
   Global UI helpers: alerts, password toggle, search, tooltips
   ════════════════════════════════════════════════════════════════ */

(function () {
    'use strict';

    /* ── Auto-dismiss alerts after 5s ── */
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.alert.alert-success, .alert.alert-info').forEach(function (el) {
            setTimeout(function () {
                el.style.transition = 'opacity .5s ease';
                el.style.opacity = '0';
                setTimeout(function () { el.remove(); }, 500);
            }, 5000);
        });
    });

    /* ── Password visibility toggle ── */
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.pw-toggle').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var input = this.closest('.pw-wrap').querySelector('input');
                if (!input) return;
                var isText = input.type === 'text';
                input.type = isText ? 'password' : 'text';
                this.innerHTML = isText
                    ? '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>'
                    : '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></svg>';
            });
        });
    });

    /* ── Table client-side search ── */
    window.tableSearch = function (inputId, tableId) {
        var input = document.getElementById(inputId);
        if (!input) return;
        input.addEventListener('input', function () {
            var q = this.value.toLowerCase();
            var rows = document.querySelectorAll('#' + tableId + ' tbody tr');
            rows.forEach(function (row) {
                row.style.display = row.innerText.toLowerCase().includes(q) ? '' : 'none';
            });
        });
    };

    /* ── Cover image preview ── */
    window.initCoverPreview = function (inputId, previewId, placeholderId) {
        var input = document.getElementById(inputId);
        if (!input) return;
        input.addEventListener('change', function () {
            var file = this.files[0];
            if (!file) return;
            var reader = new FileReader();
            reader.onload = function (e) {
                var preview = document.getElementById(previewId);
                var placeholder = document.getElementById(placeholderId);
                if (preview) { preview.src = e.target.result; preview.classList.remove('d-none'); }
                if (placeholder) placeholder.classList.add('d-none');
            };
            reader.readAsDataURL(file);
        });
    };

    /* ── Confirm dialog helper ── */
    window.confirmAction = function (msg) {
        return confirm(msg || 'Are you sure?');
    };

    /* ── Bootstrap tooltips init ── */
    document.addEventListener('DOMContentLoaded', function () {
        var tooltipEls = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        tooltipEls.forEach(function (el) {
            new bootstrap.Tooltip(el, { trigger: 'hover' });
        });
    });

    /* ── Active nav link highlight ── */
    document.addEventListener('DOMContentLoaded', function () {
        var path = window.location.pathname.toLowerCase();
        document.querySelectorAll('.re-nav .nav-link').forEach(function (link) {
            var href = (link.getAttribute('href') || '').toLowerCase();
            if (href && href !== '#' && href !== '/' && path.startsWith(href)) {
                link.classList.add('active');
            }
        });
    });

    /* ── Scroll to top button ── */
    document.addEventListener('DOMContentLoaded', function () {
        var btn = document.getElementById('scrollTopBtn');
        if (!btn) return;
        window.addEventListener('scroll', function () {
            btn.style.display = window.scrollY > 300 ? 'flex' : 'none';
        });
        btn.addEventListener('click', function () {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    });

})();