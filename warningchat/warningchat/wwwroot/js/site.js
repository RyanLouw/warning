(() => {
    "use strict";

    const loader = document.getElementById("appLoader");
    if (!loader) return;

    let pendingOperations = 0;

    function render() {
        const isBusy = pendingOperations > 0;
        document.body.classList.toggle("app-is-loading", isBusy);
        loader.classList.toggle("app-loader--visible", isBusy);
        loader.setAttribute("aria-hidden", String(!isBusy));
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
        render();
    }

    // Loading is explicit: call show/hide, or wrap an operation with run.
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
})();
