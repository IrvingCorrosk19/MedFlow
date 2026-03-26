(function (window, $) {
    'use strict';

    window.MedFlowUi = {
        init: function () {
            this.flashMessages();
            this.select2Defaults();
        },
        flashMessages: function () {
            var el = document.getElementById('medflow-flash');
            if (!el || typeof toastr === 'undefined') return;
            var s = el.getAttribute('data-success');
            var e = el.getAttribute('data-error');
            var w = el.getAttribute('data-warning');
            toastr.options = {
                closeButton: true,
                progressBar: true,
                positionClass: 'toast-top-right',
                timeOut: 4200,
                showMethod: 'fadeIn',
                hideMethod: 'fadeOut',
                newestOnTop: true
            };
            if (s) toastr.success(s);
            if (e) toastr.error(e);
            if (w) toastr.warning(w);
        },
        select2Defaults: function () {
            if (!$.fn.select2) return;
            $('select[data-select2]').each(function () {
                var $t = $(this);
                if ($t.data('select2')) return;
                $t.select2({
                    width: '100%',
                    placeholder: $t.attr('placeholder') || 'Seleccionar…'
                });
            });
        },
        confirmDelete: function (form, title) {
            if (typeof Swal === 'undefined') {
                return confirm(title || '¿Eliminar este registro?');
            }
            Swal.fire({
                title: title || '¿Confirmar?',
                text: 'Esta acción no se puede deshacer.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#2563eb',
                cancelButtonColor: '#94a3b8',
                confirmButtonText: 'Sí, eliminar',
                cancelButtonText: 'Cancelar'
            }).then(function (res) {
                if (res.isConfirmed) form.submit();
            });
            return false;
        }
    };
})(window, jQuery);
