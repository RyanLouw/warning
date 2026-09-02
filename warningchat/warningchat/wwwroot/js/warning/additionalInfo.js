(function () {
    const editorEl = document.getElementById("wsQuillEditor");
    const hidden = document.getElementById("AdditionalInfoHtml");
    const form = document.getElementById("frmAdditionalInfo");
    const btn = document.getElementById("btnSaveAdditionalInfo");
    const warningIdEl = document.getElementById("WarningId");

    if (!editorEl || !hidden || !form || !btn || !warningIdEl) return;

    if (typeof Quill === "undefined") {
        return;
    }

    const quill = new Quill(editorEl, {
        theme: "snow",
        modules: {
            toolbar: [
                [{ size: ["small", false, "large", "huge"] }],
                ["bold", "italic", "underline", "strike"],
                [{ list: "ordered" }, { list: "bullet" }],
                [{ align: [] }],
                ["link"],
                ["clean"]
            ]
        }
    });

    const initialHtml = hidden.value || "";
    if (initialHtml.trim().length > 0) {
        quill.clipboard.dangerouslyPasteHTML(initialHtml);
    }

    function getWarningId() {
        return parseInt(warningIdEl.value || "0", 10);
    }

    function showSaveMsg(text, isError) {
        const el = document.getElementById("additionalInfoSaveMsg");
        if (!el) return;

        el.style.display = "block";
        el.className = isError ? "text-danger mt-2" : "text-success mt-2";
        el.textContent = text;

        setTimeout(() => {
            el.style.display = "none";
        }, 3000);
    }

    async function saveAdditionalInfo() {
        const warningId = getWarningId();
        if (!warningId) {
            showSaveMsg("WarningId missing", true);
            return;
        }

        const html = quill.root.innerHTML || "";
        hidden.value = html;

        try {
            const res = await fetch(form.action, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    warningId: warningId,
                    additionalInfoHtml: html
                })
            });

            const data = await res.json().catch(() => ({}));

            if (!res.ok || data.success === false) {
                showSaveMsg(data.message || "Save failed", true);
                return;
            }

            showSaveMsg("Saved ✓", false);

            setTimeout(() => {
                const nextStep = document.querySelector('.ws-step[data-step="8"]');
                if (nextStep) {
                    nextStep.click();
                } else {
                    console.warn("Step 8 not found. Check your stepper. ")
                }
            }, 500);
        } catch (e) {
            console.error(e);
            showSaveMsg("Network error", true);
        }
    }

    btn.addEventListener("click", saveAdditionalInfo);

    form.addEventListener("submit", function (e) {
        e.preventDefault();
        saveAdditionalInfo();
    });
})();