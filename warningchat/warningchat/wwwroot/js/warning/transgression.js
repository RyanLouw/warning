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

            if (!warningId || warningId === "0") {
                return;
            }

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

                const response = await fetch(form.action, {
                    method: "POST",
                    body: formData,
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    }
                });

                const contentType =
                    response.headers.get("content-type") ?? "";

                let data = {};

                if (contentType.includes("application/json")) {
                    data = await response.json();
                }

                if (!response.ok || data.success === false) {
                    throw new Error(
                        data.message ||
                        `Failed to save.HTTP ${response.status} `
                    );
                }
                const savedWarningId = data.warningId || warningId;

                // Reload the wizard from the server after the Issue step is
                // updated. Category changes determine which questions appear
                // in later steps, so moving through the existing DOM would
                // leave those steps (and the overview) with stale data.
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
