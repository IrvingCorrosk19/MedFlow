/**
 * MedFlow AI — Seguridad · Roles
 * Eliminación: formularios con data-confirm-delete (SweetAlert2 vía site.js / medflow.ui.js).
 */
(function (window, $) {
    'use strict';
    window.MedFlowSecurity = window.MedFlowSecurity || {};
    window.MedFlowSecurity.roles = {
        init: function () {
            if (window.MedFlowUi) MedFlowUi.init();
        }
    };
    $(function () {
        if (window.MedFlowSecurity && MedFlowSecurity.roles) MedFlowSecurity.roles.init();
    });
})(window, jQuery);
