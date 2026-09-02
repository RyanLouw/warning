(function () {
    function initCreateQuestionBuilder() {
        const modal = document.getElementById("modalCreateQuestion");
        if (!modal) return;

        const controlType = modal.querySelector("#controlType");
        const configBuilder = modal.querySelector("#configBuilder");

        const optionsBuilder = modal.querySelector("#optionsBuilder");
        const booleanPreset = modal.querySelector("#booleanPreset");
        const numberConfig = modal.querySelector("#numberConfig");

        const optionInput = modal.querySelector("#optionInput");
        const btnAddOption = modal.querySelector("#btnAddOption");
        const optionsList = modal.querySelector("#optionsList");
        const defaultOptionSelect = modal.querySelector("#defaultOptionSelect");
        const defaultBooleanSelect = modal.querySelector("#defaultBooleanSelect");

        const numMin = modal.querySelector("#numMin");
        const numMax = modal.querySelector("#numMax");
        const numStep = modal.querySelector("#numStep");

        const hiddenJson = modal.querySelector("#DefaultConfigJson");

        // If some elements are missing, don’t crash the page
        if (!controlType || !configBuilder || !hiddenJson) return;

        // prevent double-binding
        if (modal.dataset.bound === "1") return;
        modal.dataset.bound = "1";

        let options = [];

        function hideAll() {
            configBuilder.style.display = "none";
            if (optionsBuilder) optionsBuilder.style.display = "none";
            if (booleanPreset) booleanPreset.style.display = "none";
            if (numberConfig) numberConfig.style.display = "none";
        }

        function syncDefaultSelect() {
            if (!defaultOptionSelect) return;

            const current = defaultOptionSelect.value;
            defaultOptionSelect.innerHTML = `<option value="">-- None --</option>`;

            options.forEach(o => {
                const opt = document.createElement("option");
                opt.value = o;
                opt.textContent = o;
                defaultOptionSelect.appendChild(opt);
            });

            if (options.includes(current)) defaultOptionSelect.value = current;
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
                        : (defaultBooleanSelect.value === "true" ? "Yes" : "No")
                };
            }
            else if (type === "TrueFalse") {
                cfg = {
                    options: ["True", "False"],
                    default: !defaultBooleanSelect || defaultBooleanSelect.value === ""
                        ? null
                        : (defaultBooleanSelect.value === "true" ? "True" : "False")
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

        function onControlChanged() {
            hideAll();

            // reset config value each change
            hiddenJson.value = "";

            const type = controlType.value;
            if (!type) return;

            // Only show config builder when it matters
            if (type === "Dropdown" || type === "Radio") {
                configBuilder.style.display = "";
                if (optionsBuilder) optionsBuilder.style.display = "";
                renderOptionsList();
                syncDefaultSelect();
                buildJson();
                return;
            }

            if (type === "YesNo" || type === "TrueFalse") {
                configBuilder.style.display = "";
                if (booleanPreset) booleanPreset.style.display = "";
                buildJson();
                return;
            }

            if (type === "Number") {
                configBuilder.style.display = "";
                if (numberConfig) numberConfig.style.display = "";
                buildJson();
                return;
            }

            // Text/Textarea/Checkbox/Date => no config
            buildJson();
        }

        // Bind events
        controlType.addEventListener("change", onControlChanged);

        if (btnAddOption) {
            btnAddOption.addEventListener("click", () => {
                const v = (optionInput?.value || "").trim();
                if (!v) return;
                if (options.includes(v)) {
                    optionInput.value = "";
                    return;
                }

                options.push(v);
                optionInput.value = "";

                renderOptionsList();
                syncDefaultSelect();
                buildJson();
            });
        }

        if (optionInput) {
            optionInput.addEventListener("keydown", (e) => {
                if (e.key === "Enter") {
                    e.preventDefault();
                    btnAddOption?.click();
                }
            });
        }

        defaultOptionSelect?.addEventListener("change", buildJson);
        defaultBooleanSelect?.addEventListener("change", buildJson);

        [numMin, numMax, numStep].forEach(el => el?.addEventListener("input", buildJson));

        // Reset every time modal opens (nice UX)
        modal.addEventListener("show.bs.modal", () => {
            options = [];
            if (optionInput) optionInput.value = "";
            if (defaultOptionSelect) defaultOptionSelect.value = "";
            if (defaultBooleanSelect) defaultBooleanSelect.value = "";
            if (numMin) numMin.value = "";
            if (numMax) numMax.value = "";
            if (numStep) numStep.value = "1";
            hiddenJson.value = "";
            hideAll();
        });

        // Initial
        onControlChanged();
    }

    document.addEventListener("DOMContentLoaded", initCreateQuestionBuilder);
})();