window.eiffel = {
    _handler: null,
    _modalShownHandler: null,

    init() {
        this._handler = (e) => {
            // Alt+K → open template picker
            if (e.altKey && !e.ctrlKey && !e.metaKey && e.code === 'KeyK') {
                const modal = document.getElementById('template-picker-modal');
                if (!modal) return;
                e.preventDefault();
                bootstrap.Modal.getOrCreateInstance(modal).show();
                return;
            }
            // Alt+Enter → check requirement
            if (e.altKey && e.key === 'Enter') {
                e.preventDefault();
                document.getElementById('check-btn')?.click();
                return;
            }
            // Alt+L / Alt+R → previous / next variant
            if (e.altKey && (e.code === 'KeyL' || e.code === 'KeyR')) {
                e.preventDefault();
                const radios = [...document.querySelectorAll('input[name="variant"]')];
                const idx = radios.findIndex(r => r.checked);
                const next = radios[idx + (e.code === 'KeyL' ? -1 : 1)];
                if (next) document.querySelector(`label[for="${next.id}"]`)?.click();
            }
        };
        document.addEventListener('keydown', this._handler);
        const pickerModal = document.getElementById('template-picker-modal');
        if (pickerModal) {
            this._modalShownHandler = () => {
                document.getElementById('template-search')?.focus();
            };
            pickerModal.addEventListener('shown.bs.modal', this._modalShownHandler);
        }
    },

    dispose() {
        if (this._handler) {
            document.removeEventListener('keydown', this._handler);
            this._handler = null;
        }
        const pickerModal = document.getElementById('template-picker-modal');
        if (pickerModal && this._modalShownHandler) {
            pickerModal.removeEventListener('shown.bs.modal', this._modalShownHandler);
            this._modalShownHandler = null;
        }
    },

    copyToClipboard: (text) => navigator.clipboard.writeText(text),

    focusFirstInput() {
        const el = document.querySelector(
            '#eiffel-form input:not([readonly]):not([tabindex="-1"]), #eiffel-form textarea'
        );
        el?.focus();
    },

    initTooltips() {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
            const title = el.getAttribute('title') || el.getAttribute('data-bs-original-title');
            bootstrap.Tooltip.getInstance(el)?.dispose();
            if (title) el.setAttribute('title', title);
            new bootstrap.Tooltip(el);
        });
    },

    showModal: (id) => bootstrap.Modal.getOrCreateInstance(document.getElementById(id)).show(),

    hideModal(id) {
        bootstrap.Modal.getInstance(document.getElementById(id))?.hide();
    }
};
