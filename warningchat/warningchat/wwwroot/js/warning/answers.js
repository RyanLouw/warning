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

    document.addEventListener("input", function (e) {
        const el = e.target;
        if (!el.classList.contains("ws-text")) return;

        const qid = el.dataset.qid;
        if (!qid) return;

        setHidden(qid, el.value, "");
    });

    document.addEventListener("input", function (e) {
        const el = e.target;
        if (!el.classList.contains("ws-q2")) return;

        const qid = el.dataset.qid || "2";
        setHidden(qid, el.value, "");
    });

    document.addEventListener("change", function (e) {
        const el = e.target;
        if (!el.classList.contains("ws-single")) return;

        const qid = el.dataset.qid;
        if (!qid) return;

        setHidden(qid, el.value, "");
    });

    document.addEventListener("change", function (e) {
        const el = e.target;
        if (!el.classList.contains("ws-multi")) return;

        const qid = el.dataset.qid;
        if (!qid) return;

        const max = parseInt(el.getAttribute("data-max-selected") || "0", 10);

        const selected = Array.from(el.selectedOptions).map(o => o.value);

        if (max > 0 && selected.length > max) {
            const keep = selected.slice(0, max);
            Array.from(el.options).forEach(o => {
                o.selected = keep.includes(o.value);
            });
        }

        const finalSelected = Array.from(el.selectedOptions).map(o => o.value);

        setHidden(qid, finalSelected.join(", "), JSON.stringify({ selected: finalSelected }));
    });

    document.addEventListener("change", function (e) {
        const el = e.target;
        if (!el.classList.contains("ws-radio")) return;

        const qid = el.dataset.qid;
        if (!qid) return;

        setHidden(qid, el.value, "");
    });

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
            }
            else {
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

            console.log("Saved successfullyyyyyyy", answers);

            window.warningWizard?.reloadAtStep(4);
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

    function initialSync() {
        const q2TextEl = document.getElementById("Q2_AnswerText");
        const q2JsonEl = document.getElementById("Q2_AnswerJson");

        if (q2TextEl) {
            setHidden("2", q2TextEl.value || "", q2JsonEl?.value || "");
        }

        document.querySelectorAll(".ws-text").forEach(el => {
            const qid = el.dataset.qid;
            if (qid) {
                setHidden(qid, el.value, document.getElementById(`Q${qid}_AnswerJson`)?.value || "");
            }
        });

        document.querySelectorAll(".ws-single").forEach(el => {
            const qid = el.dataset.qid;
            if (qid) {
                setHidden(qid, el.value, document.getElementById(`Q${qid}_AnswerJson`)?.value || "");
            }
        });

        document.querySelectorAll(".ws-multi").forEach(el => {
            const qid = el.dataset.qid;
            if (!qid) return;

            const jsonEl = document.getElementById(`Q${qid}_AnswerJson`);

            if (jsonEl && jsonEl.value) {
                try {
                    const obj = JSON.parse(jsonEl.value);
                    const selected = Array.isArray(obj.selected) ? obj.selected : [];

                    Array.from(el.options).forEach(opt => {
                        opt.selected = selected.includes(opt.value);
                    });

                    setHidden(qid, selected.join(", "), jsonEl.value);
                    return;
                } catch {
                }
            }

            const selected = Array.from(el.selectedOptions).map(o => o.value);
            setHidden(qid, selected.join(", "), JSON.stringify({ selected }));
        });

        document.querySelectorAll(".ws-radio:checked").forEach(el => {
            const qid = el.dataset.qid;
            if (qid) setHidden(qid, el.value, "");
        });
    }

    document.addEventListener("DOMContentLoaded", initialSync);
})();
