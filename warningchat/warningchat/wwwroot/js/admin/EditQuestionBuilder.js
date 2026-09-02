(function () {
    function initEditQuestionBuilder() {
        const modal = document.getElementById("modalEditQuestion");
        if (!modal) return;

        const questionId = modal.querySelector("#editQuestionId");
        const questionText = modal.querySelector("#editQuestionText");
        const controlType = modal.querySelector("#editQuestionControl");
        const hiddenJson = modal.querySelector("#editQuestionConfig");

        const configBuilder = modal.querySelector("#editConfigBuilder");

        const optionsBuilder = modal.querySelector("#editOptionsBuilder");
        const optionInput = modal.querySelector("#editOptionInput");
        const btnAddOption = modal.querySelector("#editBtnAddOption");
        const optionsList = modal.querySelector("#editOptionsList");
        const defaultOptionSelect = modal.querySelector("#editDefaultOptionSelect");

        const booleanPreset = modal.querySelector("#editBooleanPreset");
        const defaultBooleanSelect = modal.querySelector("#editDefaultBooleanSelect");

        const numberConfig = modal.querySelector("#editNumberConfig");
        const numMin = modal.querySelector("#editNumMin");
        const numMax = modal.querySelector("#editNumMax");
        const numStep = modal.querySelector("#editNumStep");

        if (!questionId || !questionText || !controlType || !hiddenJson || !configBuilder) return;

        if (modal.dataset.editBuilderBound === "1") return;
        modal.dataset.editBuilderBound = "1";

        let options = [];

        function safeParseJson(value) {
            if (!value || !value.trim()) return null;

            try {
                return JSON.parse(value);
            } catch {
                console.warn("Invalid DefaultConfigJson:", value);
                return null;
            }
        }

        function hideAll() {
            configBuilder.style.display = "none";

            if (optionsBuilder) optionsBuilder.style.display = "none";
            if (booleanPreset) booleanPreset.style.display = "none";
            if (numberConfig) numberConfig.style.display = "none";
        }

        function syncDefaultSelect(selectedValue) {
            if (!defaultOptionSelect) return;

            const current = selectedValue ?? defaultOptionSelect.value;

            defaultOptionSelect.innerHTML = `<option value="">-- None --</option>`;

            options.forEach(o => {
                const opt = document.createElement("option");
                opt.value = o;
                opt.textContent = o;
                defaultOptionSelect.appendChild(opt);
            });

            if (options.includes(current)) {
                defaultOptionSelect.value = current;
            }
        }

        function renderOptionsList() {
            if (!optionsList) return;

            optionsList.innerHTML = "";

            options.forEach((opt, idx) => {
                const li = document.createElement("li");
                li.className = "list-group-item d-flex justify-content-between align-items-center";

                const text = document.createElement("div");
                text.textContent = opt;

                const btnGroup = document.createElement("div");
                btnGroup.className = "btn-group btn-group-sm";

                const up = document.createElement("button");
                up.type = "button";
                up.className = "btn btn-outline-secondary";
                up.textContent = "↑";
                up.disabled = idx === 0;
                up.addEventListener("click", () => {
                    [options[idx - 1], options[idx]] = [options[idx], options[idx - 1]];
                    renderOptionsList();
                    syncDefaultSelect();
                    buildJson();
                });

                const down = document.createElement("button");
                down.type = "button";
                down.className = "btn btn-outline-secondary";
                down.textContent = "↓";
                down.disabled = idx === options.length - 1;
                down.addEventListener("click", () => {
                    [options[idx + 1], options[idx]] = [options[idx], options[idx + 1]];
                    renderOptionsList();
                    syncDefaultSelect();
                    buildJson();
                });

                const del = document.createElement("button");
                del.type = "button";
                del.className = "btn btn-outline-danger";
                del.textContent = "Remove";
                del.addEventListener("click", () => {
                    options.splice(idx, 1);
                    renderOptionsList();
                    syncDefaultSelect();
                    buildJson();
                });

                btnGroup.appendChild(up);
                btnGroup.appendChild(down);
                btnGroup.appendChild(del);

                li.appendChild(text);
                li.appendChild(btnGroup);

                optionsList.appendChild(li);
            });
        }

        function buildJson() {
            const type = controlType.value;
            let cfg = null;

            if (type === "Dropdown" || type === "Radio") {
                cfg = {
                    options: options.slice(),
                    default: defaultOptionSelect ? (defaultOptionSelect.value || null) : null
                };
            }
            else if (type === "YesNo") {
                cfg = {
                    options: ["Yes", "No"],
                    default: !defaultBooleanSelect || defaultBooleanSelect.value === ""
                        ? null
                        : defaultBooleanSelect.value === "true" ? "Yes" : "No"
                };
            }
            else if (type === "TrueFalse") {
                cfg = {
                    options: ["True", "False"],
                    default: !defaultBooleanSelect || defaultBooleanSelect.value === ""
                        ? null
                        : defaultBooleanSelect.value === "true" ? "True" : "False"
                };
            }
            else if (type === "Number") {
                cfg = {
                    min: !numMin || numMin.value === "" ? null : Number(numMin.value),
                    max: !numMax || numMax.value === "" ? null : Number(numMax.value),
                    step: !numStep || numStep.value === "" ? 1 : Number(numStep.value)
                };
            }

            hiddenJson.value = cfg ? JSON.stringify(cfg) : "";
        }

        function populateBuilderFromJson(type, jsonValue) {
            const cfg = safeParseJson(jsonValue);

            options = [];

            if (optionInput) optionInput.value = "";
            if (defaultOptionSelect) defaultOptionSelect.value = "";
            if (defaultBooleanSelect) defaultBooleanSelect.value = "";
            if (numMin) numMin.value = "";
            if (numMax) numMax.value = "";
            if (numStep) numStep.value = "1";

            hideAll();

            if (!type) {
                hiddenJson.value = "";
                return;
            }

            if (type === "Dropdown" || type === "Radio") {
                configBuilder.style.display = "";
                if (optionsBuilder) optionsBuilder.style.display = "";

                options = Array.isArray(cfg?.options) ? cfg.options : [];

                renderOptionsList();
                syncDefaultSelect(cfg?.default || "");
                buildJson();
                return;
            }

            if (type === "YesNo" || type === "TrueFalse") {
                configBuilder.style.display = "";
                if (booleanPreset) booleanPreset.style.display = "";

                if (defaultBooleanSelect && cfg?.default) {
                    const normalized = String(cfg.default).toLowerCase();

                    if (normalized === "yes" || normalized === "true") {
                        defaultBooleanSelect.value = "true";
                    }
                    else if (normalized === "no" || normalized === "false") {
                        defaultBooleanSelect.value = "false";
                    }
                }

                buildJson();
                return;
            }

            if (type === "Number") {
                configBuilder.style.display = "";
                if (numberConfig) numberConfig.style.display = "";

                if (numMin && cfg?.min !== null && cfg?.min !== undefined) numMin.value = cfg.min;
                if (numMax && cfg?.max !== null && cfg?.max !== undefined) numMax.value = cfg.max;
                if (numStep && cfg?.step !== null && cfg?.step !== undefined) numStep.value = cfg.step;

                buildJson();
                return;
            }

            // Text / Textarea / Checkbox / Date = no config
            hiddenJson.value = "";
        }

        modal.addEventListener("show.bs.modal", function (event) {
            const btn = event.relatedTarget;
            if (!btn) return;

            const id = btn.getAttribute("data-id") || "";
            const text = btn.getAttribute("data-text") || "";
            const control = btn.getAttribute("data-control") || "";
            const config = btn.getAttribute("data-config") || "";

            questionId.value = id;
            questionText.value = text;
            controlType.value = control;

            const active = (btn.getAttribute("data-active") || "").toLowerCase() === "true";
            const activeBox = modal.querySelector("#editQuestionActive");
            if (activeBox) activeBox.checked = active;

            populateBuilderFromJson(control, config);
        });

        controlType.addEventListener("change", () => {
            populateBuilderFromJson(controlType.value, "");
        });

        btnAddOption?.addEventListener("click", () => {
            const value = (optionInput?.value || "").trim();
            if (!value) return;

            if (options.includes(value)) {
                if (optionInput) optionInput.value = "";
                return;
            }

            options.push(value);

            if (optionInput) optionInput.value = "";

            renderOptionsList();
            syncDefaultSelect();
            buildJson();
        });

        optionInput?.addEventListener("keydown", e => {
            if (e.key === "Enter") {
                e.preventDefault();
                btnAddOption?.click();
            }
        });

        defaultOptionSelect?.addEventListener("change", buildJson);
        defaultBooleanSelect?.addEventListener("change", buildJson);

        [numMin, numMax, numStep].forEach(el => {
            el?.addEventListener("input", buildJson);
        });
    }

    document.addEventListener("DOMContentLoaded", initEditQuestionBuilder);
})();