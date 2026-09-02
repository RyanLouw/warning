(function () {
    const modal = document.getElementById("modalManageCategoryQuestions");
    if (!modal) return;

    const urlSetLink = '/Admin/SetCategoryQuestionLink';
    const urlGetAvailable = '/Admin/GetAvailableQuestions';
    const urlGetLinked = '/Admin/GetLinkedQuestions';

    const hiddenCategoryId = modal.querySelector("#manageCategoryId");
    const selectQuestion = modal.querySelector("#manageAddQuestionSelect");
    const chkRequired = modal.querySelector("#manageIsRequired");
    const txtSort = modal.querySelector("#manageSortOrder");
    const txtConfig = modal.querySelector("#manageConfigJson");
    const btnAdd = modal.querySelector("#btnAddQuestionToCategory");
    const tableBody = modal.querySelector("#linkedQuestionsTable tbody");
    const rulesHost = modal.querySelector("#manageRulesBuilder");

    // If core elements are missing, don't run
    if (!hiddenCategoryId || !selectQuestion || !btnAdd || !tableBody) return;

    function getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    function escapeHtml(str) {
        return String(str ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    async function getJson(url) {
        const r = await fetch(url, { credentials: "same-origin" });
        const text = await r.text();
        if (!r.ok) throw new Error("GET failed: " + r.status + " " + text.substring(0, 200));
        return text ? JSON.parse(text) : [];
    }

    async function postJson(url, data) {
        const token = getAntiForgeryToken();

        const r = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token
            },
            credentials: "same-origin",
            body: JSON.stringify(data)
        });

        const text = await r.text();
        if (!r.ok) {
            console.error("POST failed", r.status, text);
            throw new Error(`POST failed: ${r.status} - ${text.substring(0, 500)}`);
        }
        return text ? JSON.parse(text) : {};
    }

    // ---------- RULE BUILDER ----------
    function setJsonPreview(obj) {
        if (!txtConfig) return;
        txtConfig.value = obj ? JSON.stringify(obj, null, 2) : "";
    }

    function buildRulesUI(controlType) {
        if (!rulesHost) return;

        rulesHost.innerHTML = "";
        let rules = { rules: [] };

        if (controlType === "Text" || controlType === "Textarea") {
            rulesHost.innerHTML = `
    <div class="mb-2">
        <div class="fw-semibold small">Text answer rules</div>
        <div class="text-muted small">
            Limit how short or long the answer may be.
        </div>
    </div>

    <div class="row g-2">
        <div class="col-6">
            <label class="form-label small">Minimum characters</label>
            <input type="number"
                   class="form-control form-control-sm"
                   id="ruleMinLen"
                   min="0"
                   placeholder="No minimum" />
        </div>

        <div class="col-6">
            <label class="form-label small">Maximum characters</label>
            <input type="number"
                   class="form-control form-control-sm"
                   id="ruleMaxLen"
                   min="0"
                   placeholder="No maximum" />
        </div>

    </div>
`;

            const minEl = rulesHost.querySelector("#ruleMinLen");
            const maxEl = rulesHost.querySelector("#ruleMaxLen");
            const patEl = rulesHost.querySelector("#rulePattern");

            function sync() {
                rules.rules = [];
                if (minEl?.value) rules.rules.push({ type: "text", op: "minLength", value: Number(minEl.value) });
                if (maxEl?.value) rules.rules.push({ type: "text", op: "maxLength", value: Number(maxEl.value) });
                if (patEl?.value?.trim()) rules.rules.push({ type: "text", op: "pattern", value: patEl.value.trim() });
                setJsonPreview(rules);
            }

            [minEl, maxEl, patEl].forEach(x => x?.addEventListener("input", sync));
            sync();
            return;
        }

        if (controlType === "Date") {
            rulesHost.innerHTML = `
                <div class="form-check">
                  <input class="form-check-input" type="checkbox" id="ruleNoFuture" checked>
                  <label class="form-check-label" for="ruleNoFuture">Date cannot be in the future</label>
                </div>
                <div class="row g-2 mt-2">
                  <div class="col-6">
                    <label class="form-label small">Min Date</label>
                    <input type="date" class="form-control form-control-sm" id="ruleMinDate" />
                  </div>
                  <div class="col-6">
                    <label class="form-label small">Max Date</label>
                    <input type="date" class="form-control form-control-sm" id="ruleMaxDate" />
                  </div>
                </div>
            `;

            const noFuture = rulesHost.querySelector("#ruleNoFuture");
            const minDate = rulesHost.querySelector("#ruleMinDate");
            const maxDate = rulesHost.querySelector("#ruleMaxDate");

            function sync() {
                rules.rules = [];
                if (noFuture?.checked) rules.rules.push({ type: "date", op: "max", value: "today" });
                if (minDate?.value) rules.rules.push({ type: "date", op: "min", value: minDate.value });
                if (maxDate?.value) rules.rules.push({ type: "date", op: "max", value: maxDate.value });
                setJsonPreview(rules);
            }

            [noFuture, minDate, maxDate].forEach(x => x?.addEventListener("change", sync));
            sync();
            return;
        }

        if (controlType === "Number") {
            rulesHost.innerHTML = `
                <div class="row g-2">
                  <div class="col-6">
                    <label class="form-label small">Min</label>
                    <input type="number" class="form-control form-control-sm" id="ruleMinNum" />
                  </div>
                  <div class="col-6">
                    <label class="form-label small">Max</label>
                    <input type="number" class="form-control form-control-sm" id="ruleMaxNum" />
                  </div>
                </div>
            `;

            const minNum = rulesHost.querySelector("#ruleMinNum");
            const maxNum = rulesHost.querySelector("#ruleMaxNum");

            function sync() {
                rules.rules = [];
                if (minNum?.value) rules.rules.push({ type: "number", op: "min", value: Number(minNum.value) });
                if (maxNum?.value) rules.rules.push({ type: "number", op: "max", value: Number(maxNum.value) });
                setJsonPreview(rules);
            }

            [minNum, maxNum].forEach(x => x?.addEventListener("input", sync));
            sync();
            return;
        }

        if (controlType === "Dropdown" || controlType === "Radio") {
            rulesHost.innerHTML = `
                <div class="form-check">
                  <input class="form-check-input" type="checkbox" id="ruleAllowMulti">
                  <label class="form-check-label" for="ruleAllowMulti">Allow multiple selections</label>
                </div>
            `;

            const allowMulti = rulesHost.querySelector("#ruleAllowMulti");
            function sync() {
                rules.rules = [];
                if (allowMulti?.checked) rules.rules.push({ type: "select", op: "allowMultiple", value: true });
                setJsonPreview(rules);
            }

            allowMulti?.addEventListener("change", sync);
            sync();
            return;
        }

        rulesHost.innerHTML = `<div class="text-muted">No rules available for this type.</div>`;
        setJsonPreview(null);
    }

    selectQuestion.addEventListener("change", () => {
        const opt = selectQuestion.options[selectQuestion.selectedIndex];
        const controlType = opt?.dataset?.control || "";
        buildRulesUI(controlType);
    });

    // ---------- RENDERING ----------
    function renderAvailableQuestions(items) {
        selectQuestion.innerHTML = `<option value="">-- Select --</option>`;

        (items || []).forEach(q => {
            const id = q.questionId ?? q.QuestionId;
            const text = q.questionText ?? q.QuestionText;
            const control = q.controlType ?? q.ControlType;

            const op = document.createElement("option");
            op.value = id;
            op.textContent = text;
            op.dataset.control = control || "";
            selectQuestion.appendChild(op);
        });

        selectQuestion.dispatchEvent(new Event("change"));
    }

    function renderLinkedQuestions(rows) {
        tableBody.innerHTML = "";

        if (!rows || rows.length === 0) {
            tableBody.innerHTML = `<tr><td colspan="5" class="text-muted">No linked questions.</td></tr>`;
            return;
        }

        rows.forEach(r => {
            const questionId = r.QuestionId ?? r.questionId;
            const questionText = r.QuestionText ?? r.questionText;
            const controlType = r.ControlType ?? r.controlType;
            const isRequired = r.IsRequired ?? r.isRequired;
            const sortOrder = r.SortOrder ?? r.sortOrder;
            const configJson = r.ConfigJson ?? r.configJson;

            const isProtectedQuestion = [1, 2, 3].includes(Number(questionId));

            const tr = document.createElement("tr");
            tr.innerHTML = `
            <td>${escapeHtml(questionText)}</td>
            <td>${escapeHtml(controlType)}</td>
            <td class="text-center">${isRequired ? "Yes" : "No"}</td>
            <td>${sortOrder}</td>
            <td class="text-end">
              <button type="button"
                      class="btn btn-sm btn-outline-danger"
                      ${isProtectedQuestion ? "disabled title='Questions 1 to 3 cannot be removed'" : ""}>
                Remove
              </button>
            </td>
        `;

            tr.querySelector("button").addEventListener("click", async () => {
                const categoryId = parseInt(hiddenCategoryId.value, 10);
                if (!Number.isFinite(categoryId) || categoryId <= 0) return;

                btnAdd.disabled = true;

                await postJson(urlSetLink, {
                    categoryId,
                    questionId,
                    isActive: false,
                    isRequired,
                    sortOrder,
                    configJson: configJson ?? null
                });

                btnAdd.disabled = false;
                await loadAll();
            });

            tableBody.appendChild(tr);
        });
    }

    async function loadAll() {
        const categoryId = parseInt(hiddenCategoryId.value, 10);
        console.log("manageCategoryId =", hiddenCategoryId.value);

        if (!Number.isFinite(categoryId) || categoryId <= 0) return;

        const available = await getJson(`${urlGetAvailable}?categoryId=${categoryId}`);
        renderAvailableQuestions(available);

        const linked = await getJson(`${urlGetLinked}?categoryId=${categoryId}`);
        renderLinkedQuestions(linked);
    }

    btnAdd.addEventListener("click", async () => {
        const categoryId = parseInt(hiddenCategoryId.value, 10);
        const questionId = parseInt(selectQuestion.value, 10);

        if (!Number.isFinite(categoryId) || categoryId <= 0) return alert("Category not set.");
        if (!Number.isFinite(questionId) || questionId <= 0) return alert("Please select a question.");

        const cfg = (txtConfig?.value || "").trim();
        if (cfg) {
            try { JSON.parse(cfg); }
            catch { return alert("Generated config JSON is invalid (should not happen)."); }
        }

        btnAdd.disabled = true;

        await postJson(urlSetLink, {
            categoryId,
            questionId,
            isActive: true,
            isRequired: chkRequired?.checked ?? false,
            sortOrder: Number(txtSort?.value || 0),
            configJson: cfg || null
        });

        selectQuestion.value = "";
        if (chkRequired) chkRequired.checked = false;
        if (txtSort) txtSort.value = "0";
        if (txtConfig) txtConfig.value = "";

        btnAdd.disabled = false;
        await loadAll();
    });

    modal.addEventListener("shown.bs.modal", () => {
        loadAll().catch(err => console.error(err));
    });
})();