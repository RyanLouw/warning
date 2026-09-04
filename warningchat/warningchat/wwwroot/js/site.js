(() => {
    "use strict";

    const loader = document.getElementById("appLoader");
    if (!loader) return;

    let pendingOperations = 0;
    const minimumVisibleTime = 350;
    let visibleSince = 0;
    let hideTimer = null;

    function setPageInactive(isInactive) {
        Array.from(document.body.children).forEach(element => {
            if (element === loader) return;

            if (isInactive && !element.hasAttribute("inert")) {
                element.setAttribute("inert", "");
                element.dataset.appLoaderInert = "true";
            } else if (!isInactive && element.dataset.appLoaderInert === "true") {
                element.removeAttribute("inert");
                delete element.dataset.appLoaderInert;
            }
        });
    }

    function showLoader() {
        window.clearTimeout(hideTimer);
        hideTimer = null;
        visibleSince = performance.now();
        document.body.classList.add("app-is-loading");
        loader.classList.add("app-loader--visible");
        loader.setAttribute("aria-hidden", "false");
        setPageInactive(true);
    }

    function hideLoader() {
        if (pendingOperations > 0) return;

        document.body.classList.remove("app-is-loading");
        loader.classList.remove("app-loader--visible");
        loader.setAttribute("aria-hidden", "true");
        setPageInactive(false);
        visibleSince = 0;
        hideTimer = null;
    }

    function render() {
        if (pendingOperations > 0) {
            window.clearTimeout(hideTimer);
            hideTimer = null;
            if (!loader.classList.contains("app-loader--visible")) showLoader();
            return;
        }

        const remainingTime = minimumVisibleTime - (performance.now() - visibleSince);
        window.clearTimeout(hideTimer);
        hideTimer = window.setTimeout(hideLoader, Math.max(0, remainingTime));
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
        window.clearTimeout(hideTimer);
        hideLoader();
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

    // Submit only fires after native validation succeeds. Waiting until event
    // dispatch completes avoids leaving the loader open for AJAX forms that
    // call preventDefault and then use the fetch/XHR hooks below.
    document.addEventListener("submit", event => {
        queueMicrotask(() => {
            if (!event.defaultPrevented) show();
        });
    }, true);

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

        queueMicrotask(() => {
            if (!event.defaultPrevented) show();
        });
    }, true);

    // A page restored from the back-forward cache can retain its old busy DOM.
    window.addEventListener("pageshow", reset);
})();
