(function () {
    const cb = document.getElementById("IsSopNonCompliance");
    const ddl = document.getElementById("SelectedSOPDocumentId");
    const btn = document.getElementById("btnSaveSop");

    if (!cb || !ddl || !btn) {
        console.warn("Missing SOP elements:", { cb, ddl, btn });
        return;
    }

    let sopSelect = null;

    if (window.TomSelect) {
        sopSelect = new TomSelect("#SelectedSOPDocumentId", {
            create: false,
            allowEmptyOption: true,
            placeholder: "Search SOP...",
            maxOptions: 500,
            searchField: ["text"],
            sortField: {
                field: "text",
                direction: "asc"
            }
        });
    }

    function sync() {
        const isDisabled = cb.checked;

        if (sopSelect) {
            if (isDisabled) {
                sopSelect.clear();
                sopSelect.disable();
            } else {
                sopSelect.enable();
            }
        } else {
            ddl.disabled = isDisabled;
            if (isDisabled) ddl.value = "";
        }
    }

    cb.addEventListener("change", sync);
    sync();

    const SOP_QUESTION_ID = 3;
    const saveUrl = "/Transgression/SaveAnswer";

    async function postJson(url, payload) {
        const res = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        const contentType = res.headers.get("content-type") || "";
        const body = contentType.includes("application/json")
            ? await res.json()
            : await res.text();

        if (!res.ok) {
            throw new Error(`HTTP ${res.status} ${res.statusText} | ${typeof body === "string" ? body : JSON.stringify(body)}`);
        }

        return body;
    }

    function getWarningId() {
        return parseInt(document.getElementById("WarningId")?.value || "0", 10);
    }

    function showSopMsg(text, isError) {
        const el = document.getElementById("sopSaveMsg");
        if (!el) return;

        el.style.display = "block";
        el.className = isError ? "text-danger mt-2" : "text-success mt-2";
        el.textContent = text;

        if (!isError) {
            setTimeout(() => {
                el.style.display = "none";
                el.textContent = "";
            }, 2000);
        }
    }

    async function saveSopAnswer() {
        const warningId = getWarningId();
        if (!warningId || warningId <= 0) {
            showSopMsg("WarningId is missing.", true);
            return;
        }

        const isNotApplicable = cb.checked;

        let sopId = null;
        let sopName = null;

        if (!isNotApplicable) {
            const selectedValue = sopSelect ? sopSelect.getValue() : ddl.value;
            sopId = parseInt(selectedValue || "0", 10);

            if (!sopId) {
                showSopMsg("Please select an SOP, or tick 'SOP Not Applicable'.", true);
                return;
            }

            const opt = ddl.querySelector(`option[value="${selectedValue}"]`);
            sopName = opt?.dataset?.name || opt?.text?.trim() || null;
        }

        const dto = {
            warningId: warningId,
            questionId: SOP_QUESTION_ID,
            answerText: isNotApplicable ? null : sopName,
            answerJson: JSON.stringify({
                notApplicable: isNotApplicable,
                sopDocumentId: sopId,
                sopDocumentName: sopName
            })
        };

        try {
            const data = await postJson(saveUrl, dto);

            if (data?.success) {
                const nextStep = document.querySelector('.ws-step[data-step="5"]');
                if (nextStep) nextStep.click();
            } else {
                showSopMsg(data?.message || "Save failed.", true);
            }
        } catch (e) {
            showSopMsg(e.message || ("Save failed: " + e), true);
            console.log(e);
        }
    }

    btn.addEventListener("click", saveSopAnswer);
})();