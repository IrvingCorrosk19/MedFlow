$(function () {
    if (window.MedFlowUi) {
        MedFlowUi.init();
    }
    $('form[data-confirm-delete]').on('submit', function (e) {
        e.preventDefault();
        var form = this;
        if (window.MedFlowUi && MedFlowUi.confirmDelete) {
            MedFlowUi.confirmDelete(form);
        } else {
            if (confirm('¿Eliminar este registro?')) form.submit();
        }
    });
});
