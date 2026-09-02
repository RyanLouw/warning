(function () {
    const btn = document.getElementById("btnMarkNew");
    if (!btn) return;

    const saveUrl = '/Transgression/UpdateStatus';

    function showMsg(text, isError) {
        const el = document.getElementById("statusSaveMsg");
        if (!el) return;

        el.style.display = "block";
        el.className = isError ? "text-danger mt-2" : "text-success mt-2";
        el.textContent = text;

        setTimeout(() => { el.style.display = "none"; }, 3500);
    }

    function addWorkingDays(startDate, workingDaysToAdd) {
        const d = new Date(startDate.getFullYear(), startDate.getMonth(), startDate.getDate());
        let added = 0;

        while (added < workingDaysToAdd) {
            d.setDate(d.getDate() + 1);

            const day = d.getDay(); // 0=Sun, 6=Sat
            const isWeekend = (day === 0 || day === 6);
            if (!isWeekend) added++;
        }

        return d;
    }
    function isFlagTrue(id) {
        const el = document.getElementById(id);
        return el && el.value === "1";
    }

    function getMissingRequired() {
        const missing = [];
        if (!isFlagTrue("reqHasEmployee")) missing.push("Employee Name");
        if (!isFlagTrue("reqHasCategory")) missing.push("Transgression Type");
        if (!isFlagTrue("reqHasDates")) missing.push("Date(s)");
        if (!isFlagTrue("reqHasDescription")) missing.push("Description");
        if (!isFlagTrue("reqHasSop")) missing.push("SOP Name");
        return missing;
    }

    function toIsoDateOnly(dateObj) {
        const y = dateObj.getFullYear();
        const m = String(dateObj.getMonth() + 1).padStart(2, "0");
        const d = String(dateObj.getDate()).padStart(2, "0");
        return `${y}-${m}-${d}`;
    }

    async function postJson(url, payload) {
        console.log("POSTING TO:", url, payload);

        const res = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        const data = await res.json().catch(() => ({}));
        if (!res.ok || data.success === false) {
            throw new Error(data.message || `HTTP ${res.status}`);
        }
        return data;
    }

    function getWarningIdFromUrl() {
        const segments = window.location.pathname.split("/").filter(Boolean);
        const last = segments[segments.length - 1];
        const id = Number.parseInt(last, 10);
        return Number.isInteger(id) && id > 0 ? id : 0;
    }

    async function markAsNew() {
        const missing = getMissingRequired();
        if (missing.length > 0) {
            btn.disabled = true;
            showMsg("Please complete: " + missing.join(", "), true);
            return;
        }

        const warningId = getWarningIdFromUrl();
        if (!warningId) {
            showMsg("WarningId not found in URL.", true);
            return;
        }

        const due = addWorkingDays(new Date(), 5);
        const dto = {
            warningId: warningId,
            status: "New",
            dueDate: toIsoDateOnly(due)
        };

        try {
            btn.disabled = true;
            const oldText = btn.textContent;
            btn.textContent = "Saving...";

            await postJson(saveUrl, dto);

            showMsg(`Saved ✓ Status = New, Due = ${dto.dueDate}`, false);

            btn.textContent = oldText;
            btn.disabled = false;
            window.location.href = "/";
        } catch (e) {
            console.error(e);
            showMsg(e.message || "Save failed.", true);
            btn.disabled = false;
            btn.textContent = "Submit to Legal";
        }
    }

    (function initButtonState() {
        const missing = getMissingRequired();
        btn.disabled = missing.length > 0;
    })();

    btn.addEventListener("click", markAsNew);
})();
(function () {
    function getWarningId() {
        return parseInt(document.getElementById("WarningId")?.value || "0", 10);
    }

    function setHidden(qid, answerText, answerJson) {
        const txt = document.getElementById(`Q${qid}_AnswerText`);
        const json = document.getElementById(`Q${qid}_AnswerJson`);

        if (txt) txt.value = answerText ?? "";
        if (json) json.value = answerJson ?? "";
    }

    async function loadOverviewStep(warningId) {
        const res = await fetch(`/Transgression/GetOverview?warningId=${warningId}`);
        if (!res.ok) {
            throw new Error("Failed to load overview");
        }

        const html = await res.text();
        const container = document.getElementById("wsStepContent");
        if (container) {
            container.innerHTML = html;
        }
    }

    function collectAnswers() {
        const warningId = getWarningId();
        if (!warningId || warningId <= 0) return { warningId: 0, answers: [] };

        const qidEls = document.querySelectorAll(`input[type="hidden"][id^="Q"][id$="_QuestionId"]`);
        const answers = [];

        qidEls.forEach(qEl => {
            const qid = parseInt(qEl.value || "0", 10);
            if (!qid) return;

            let answerText = "";
            let answerJson = "";

            if (qid === 2) {
                const txtEl = document.getElementById("Q2_AnswerText");
                const jsonEl = document.getElementById("Q2_AnswerJson");

                answerText = txtEl ? (txtEl.value ?? "") : "";
                answerJson = jsonEl ? (jsonEl.value ?? "") : "";
            } else {
                const textInput = document.querySelector(`.ws-text[data-qid="${qid}"]`);
                const singleSelect = document.querySelector(`.ws-single[data-qid="${qid}"]`);
                const multiSelect = document.querySelector(`.ws-multi[data-qid="${qid}"]`);
                const radioChecked = document.querySelector(`input[name="Q${qid}_Radio"]:checked`);

                if (textInput) {
                    answerText = textInput.value ?? "";
                } else if (singleSelect) {
                    answerText = singleSelect.value ?? "";
                } else if (multiSelect) {
                    const selected = Array.from(multiSelect.selectedOptions).map(o => o.value);
                    answerText = selected.join(", ");
                    answerJson = JSON.stringify({ selected });
                } else if (radioChecked) {
                    answerText = radioChecked.value ?? "";
                } else {
                    const txtEl = document.getElementById(`Q${qid}_AnswerText`);
                    const jsonEl = document.getElementById(`Q${qid}_AnswerJson`);
                    answerText = txtEl ? (txtEl.value ?? "") : "";
                    answerJson = jsonEl ? (jsonEl.value ?? "") : "";
                }
            }

            answers.push({ warningId, questionId: qid, answerText, answerJson });
        });

        return { warningId, answers };
    }

    async function saveAll() {
        const warningId = getWarningId();
        if (!warningId || warningId <= 0) {
            alert("Please create/save the warning first (Step 1) before saving this step.");
            return;
        }

        const { answers } = collectAnswers();

        if (!answers.length) {
            alert("Nothing to save.");
            return;
        }

        try {
            const res = await fetch("/Transgression/SaveAnswers", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(answers)
            });

            const data = await res.json().catch(() => ({}));

            if (!res.ok || data.success === false) {
                alert(data.message || "Failed to save answers.");
                return;
            }

            await loadOverviewStep(warningId);

            const nextStep = document.querySelector('.ws-step[data-step="4"]');
            if (nextStep) {
                nextStep.click();
            } else {
                console.warn("Step 4 not found in wsStepper");
            }
        } catch (err) {
            console.error(err);
            alert("Network/error while saving.");
        }
    }

    document.addEventListener("click", function (e) {
        const btn = e.target.closest("#wsSaveDescriptionBtn");
        if (!btn) return;

        e.preventDefault();
        saveAll();
    });
})();