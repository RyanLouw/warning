(function () {
    "use strict";

    function initialise() {
        initialiseTransgressionForm();
        initialiseCategorySearch();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initialise);
    } else {
        initialise();
    }

    function initialiseTransgressionForm() {
        const form = document.getElementById("frmTransgression");

        if (!form || form.dataset.initialised === "true") {
            return;
        }

        form.dataset.initialised = "true";

        form.addEventListener("submit", async function (event) {
            const warningIdElement =
                form.querySelector('input[name="WarningId"]');

            const warningId =
                warningIdElement?.value?.trim() ?? "";

            event.preventDefault();

            const submitButton =
                form.querySelector('button[type="submit"]');

            const originalButtonText =
                submitButton?.textContent?.trim() ||
                "Save & Continue";

            try {
                if (submitButton) {
                    submitButton.disabled = true;
                    submitButton.textContent = "Saving...";
                }

                const formData = new FormData(form);
                formData.set("responseMode", "json");

                const response = await fetch(form.action, {
                    method: "POST",
                    body: formData,
                    headers: {
                        "X-Requested-With": "XMLHttpRequest",
                        "Accept": "application/json"
                    }
                });

                const contentType =
                    response.headers.get("content-type") ?? "";

                let data = {};

                if (contentType.includes("application/json")) {
                    data = await response.json();
                }

                const success = data.success ?? data.Success;
                const message = data.message ?? data.Message;
                let savedWarningId = data.warningId ?? data.WarningId;

                if (!contentType.includes("application/json") && response.redirected && response.url) {
                    const redirected = new URL(response.url, window.location.href);
                    const wizardRoute = redirected.pathname.match(/^\/Transgression(?:\/Index)?(?:\/(\d+))?\/?$/i);
                    if (redirected.origin === new URL(window.location.href).origin && wizardRoute) {
                        savedWarningId = redirected.searchParams.get("id") || wizardRoute[1];
                    }
                }

                if (!response.ok || success === false) {
                    throw new Error(message || `Failed to save. HTTP ${response.status}`);
                }
                if (!savedWarningId || !/^[1-9]\d*$/.test(String(savedWarningId)) ||
                    (contentType.includes("application/json") && success !== true)) {
                    console.error("Unexpected issue save response", {
                        status: response.status, contentType, redirected: response.redirected,
                        url: response.url, data
                    });
                    throw new Error(contentType.includes("application/json")
                        ? "The save response is missing the issue ID. Refresh before trying again to avoid creating a duplicate."
                        : "The save returned a page instead of confirmation. Refresh and check whether the issue was created before trying again.");
                }

                const url = new URL(window.location.href);
                url.searchParams.set("id", savedWarningId);

                sessionStorage.setItem("warningWizardActiveStep", "2");
                window.location.assign(url.toString());
            } catch (error) {
                console.error(
                    "Could not save transgression details:",
                    error
                );

                alert(
                    error.message ||
                    "Failed to save transgression details."
                );
            } finally {
                if (submitButton) {
                    submitButton.disabled = false;
                    submitButton.textContent = originalButtonText;
                }
            }
        });
    }

    function initialiseCategorySearch() {
        const searchInput =
            document.getElementById("categorySearch");

        const searchButton =
            document.getElementById("btnSearchCategories");

        const clearButton =
            document.getElementById("clearCategorySearch");

        const categoryGrid =
            document.getElementById("categoryGrid");

        const emptyState =
            document.getElementById("categoryEmptyState");

        if (
            !searchInput ||
            !searchButton ||
            !clearButton ||
            !categoryGrid ||
            !emptyState
        ) {
            console.warn("Category search controls could not be found.", {
                searchInput,
                searchButton,
                clearButton,
                categoryGrid,
                emptyState
            });

            return;
        }

        if (searchInput.dataset.initialised === "true") {
            return;
        }

        searchInput.dataset.initialised = "true";

        function normalise(value) {
            return String(value ?? "")
                .trim()
                .toLowerCase()
                .replace(/\s+/g, " ");
        }

        function filterCategories() {
            const searchTerm = normalise(searchInput.value);

            const categoryCards =
                categoryGrid.querySelectorAll(".ws-category-card");

            let visibleCount = 0;

            categoryCards.forEach(function (card) {
                const categoryTitle =
                    card.querySelector(".ws-category-title")
                        ?.textContent ?? "";

                const categoryName = normalise(
                    card.dataset.categoryName ||
                    categoryTitle
                );

                const matches =
                    searchTerm === "" ||
                    categoryName.includes(searchTerm);

                if (matches) {
                    card.style.removeProperty("display");
                    visibleCount++;
                } else {
                    card.style.setProperty(
                        "display",
                        "none",
                        "important"
                    );
                }
            });

            emptyState.classList.toggle(
                "d-none",
                visibleCount > 0
            );

            console.log("Category search result:", {
                searchTerm,
                totalCategories: categoryCards.length,
                visibleCategories: visibleCount
            });
        }

        searchButton.addEventListener(
            "click",
            filterCategories
        );

        clearButton.addEventListener("click", function () {
            searchInput.value = "";
            filterCategories();
            searchInput.focus();
        });

        searchInput.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                event.preventDefault();
                filterCategories();
            }

            if (event.key === "Escape") {
                event.preventDefault();

                searchInput.value = "";
                filterCategories();
                searchInput.focus();
            }
        });

        searchInput.addEventListener(
            "input",
            filterCategories
        );

        filterCategories();
    }
})();
