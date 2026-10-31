// Online/offline tracking and "new version available" handling.
window.appStatus = {
    init(dotnetRef) {
        window.addEventListener('online', () => dotnetRef.invokeMethodAsync('OnOnlineChanged', true));
        window.addEventListener('offline', () => dotnetRef.invokeMethodAsync('OnOnlineChanged', false));

        const reg = window.swRegistration;
        if (reg) {
            reg.then(registration => {
                const notify = () => dotnetRef.invokeMethodAsync('OnUpdateAvailable');
                // An update was already downloaded before the app started.
                if (registration.waiting && navigator.serviceWorker.controller) notify();
                registration.addEventListener('updatefound', () => {
                    const worker = registration.installing;
                    worker?.addEventListener('statechange', () => {
                        if (worker.state === 'installed' && navigator.serviceWorker.controller) notify();
                    });
                });
                // Check for updates periodically while the app is open.
                setInterval(() => registration.update().catch(() => { }), 60 * 60 * 1000);
            }).catch(() => { });
        }
        return navigator.onLine;
    },

    async applyUpdate() {
        const registration = await window.swRegistration;
        if (registration?.waiting) {
            navigator.serviceWorker.addEventListener('controllerchange', () => location.reload(), { once: true });
            registration.waiting.postMessage({ type: 'SKIP_WAITING' });
        } else {
            location.reload();
        }
    }
};
