window.vtl = {
    _observer: null,

    setup(dotnetRef) {
        const sentinel = document.getElementById('vtl-sentinel');
        if (!sentinel) return;
        this._observer = new IntersectionObserver((entries) => {
            if (entries[0].isIntersecting) {
                dotnetRef.invokeMethodAsync('LoadMore');
            }
        }, { rootMargin: '300px' });
        this._observer.observe(sentinel);
    },

    scrollToToday() {
        const el = document.querySelector('.vtl-today-row');
        if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    },

    dispose() {
        if (this._observer) {
            this._observer.disconnect();
            this._observer = null;
        }
    }
};
