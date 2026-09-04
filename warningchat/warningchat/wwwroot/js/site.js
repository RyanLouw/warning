(() => {
    "use strict";

    const loader = document.getElementById("appLoader");
    if (!loader) return;

    let pendingOperations = 0;
    let showTimer = null;
    let navigationInProgress = false;

    function render() {
        const isBusy = pendingOperations > 0;
        document.body.classList.toggle("app-is-loading", isBusy);
        loader.setAttribute("aria-hidden", String(!isBusy));

        if (!isBusy) {
            clearTimeout(showTimer);
            showTimer = null;
            loader.classList.remove("app-loader--visible");
            return;
        }

        if (!showTimer && !loader.classList.contains("app-loader--visible")) {
            showTimer = window.setTimeout(() => {
                loader.classList.add("app-loader--visible");
                showTimer = null;
            }, 120);
        }
    }

    function show() {
        pendingOperations += 1;
        render();
    }

    function hide() {
        pendingOperations = Math.max(0, pendingOperations - 1);
        render();
    }

    function reset() {
        pendingOperations = 0;
        navigationInProgress = false;
        render();
    }

    function beginNavigation() {
        if (navigationInProgress) return;

        navigationInProgress = true;
        show();

        // Navigation leaves the current document in place while the server
        // builds the next page, so reveal the loader immediately rather than
        // waiting for the short-request delay used by fetch and XHR.
        clearTimeout(showTimer);
        showTimer = null;
        loader.classList.add("app-loader--visible");
    }

    window.appLoader = {
        show,
        hide,
        reset,
        async run(operation) {
            show();
            try {
                return await operation();
            } finally {
                hide();
            }
        }
    };

    const originalFetch = window.fetch.bind(window);
    window.fetch = async (...args) => {
        show();
        try {
            return await originalFetch(...args);
        } finally {
            hide();
        }
    };

    const originalXhrSend = XMLHttpRequest.prototype.send;
    XMLHttpRequest.prototype.send = function (...args) {
        show();
        this.addEventListener("loadend", hide, { once: true });

        try {
            return originalXhrSend.apply(this, args);
        } catch (error) {
            hide();
            throw error;
        }
    };

    // Listen during bubbling so a form's submit handler can prevent the native
    // navigation first. AJAX submissions are covered by the fetch/XHR hooks;
    // treating them as native submissions as well would leave an unmatched
    // loader operation after the request completes.
    document.addEventListener("submit", event => {
        if (!event.defaultPrevented) beginNavigation();
    });

    document.addEventListener("click", event => {
        const link = event.target.closest("a[href]");
        if (!link || event.defaultPrevented || event.button !== 0) return;
        if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        if (link.target && link.target.toLowerCase() !== "_self") return;
        if (link.hasAttribute("download") || link.dataset.bsToggle) return;

        const href = link.getAttribute("href") || "";
        if (!href || href.startsWith("#") || href.startsWith("javascript:")) return;

        const destination = new URL(link.href, window.location.href);
        if (destination.origin !== window.location.origin) return;

        if (!event.defaultPrevented) beginNavigation();
    });

    // Covers controller requests started without clicking a normal link, such
    // as browser reloads, location changes, and history navigation. Calls are
    // idempotent because a link or form may already have started the loader.
    window.addEventListener("beforeunload", beginNavigation);

    // A page restored from the back-forward cache can retain its old busy DOM.
    window.addEventListener("pageshow", reset);
})();
