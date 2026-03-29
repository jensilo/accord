window.eiffel = {
    _handler: null,

    init() {
        this._handler = (e) => {
            // Ctrl+F → open template picker
            if (e.ctrlKey && !e.metaKey && !e.shiftKey && e.key === 'f') {
                const modal = document.getElementById('template-picker-modal');
                if (!modal) return;
                e.preventDefault();
                bootstrap.Modal.getOrCreateInstance(modal).show();
                setTimeout(() => document.getElementById('template-search')?.focus(), 150);
                return;
            }
            // Alt+Enter → check requirement
            if (e.altKey && e.key === 'Enter') {
                e.preventDefault();
                document.getElementById('check-btn')?.click();
                return;
            }
            // Alt+← / Alt+→ → previous / next variant
            if (e.altKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
                e.preventDefault();
                const radios = [...document.querySelectorAll('input[name="variant"]')];
                const idx = radios.findIndex(r => r.checked);
                const next = radios[idx + (e.key === 'ArrowLeft' ? -1 : 1)];
                if (next) document.querySelector(`label[for="${next.id}"]`)?.click();
            }
        };
        document.addEventListener('keydown', this._handler);
    },

    dispose() {
        if (this._handler) {
            document.removeEventListener('keydown', this._handler);
            this._handler = null;
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
            bootstrap.Tooltip.getOrCreateInstance(el);
        });
    },

    showModal: (id) => bootstrap.Modal.getOrCreateInstance(document.getElementById(id)).show(),

    hideModal(id) {
        bootstrap.Modal.getInstance(document.getElementById(id))?.hide();
    }
};
