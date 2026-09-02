(function () {
    const form = document.getElementById('frmPreviousOffences');
    const cb = document.getElementById('hasPreviousOffences');
    const fileInput = document.getElementById('previousOffenceFile');
    const btnChoose = document.getElementById('btnChooseFiles');
    const list = document.getElementById('wsSelectedFiles');
    const btnSave = document.getElementById('btnSavePreviousOffences');
    const msg = document.getElementById('previousOffencesSaveMsg');
    const warningIdEl = document.getElementById('WarningId');

    if (!form || !cb || !fileInput || !btnSave) return;

    function getWarningId() {
        return parseInt(warningIdEl?.value || "0", 10);
    }

    function showMsg(text, isError) {
        if (!msg) return;
        msg.style.display = "block";
        msg.textContent = text;
        msg.style.color = isError ? "#b02a37" : "#6c757d";
    }

    function clearMsg() {
        if (!msg) return;
        msg.style.display = "none";
        msg.textContent = "";
    }

    function setEnabled(enabled) {
        fileInput.disabled = !enabled;

        if (btnChoose) {
            btnChoose.style.opacity = enabled ? "1" : ".55";
            btnChoose.style.pointerEvents = enabled ? "auto" : "none";
        }

        if (!enabled) {
            fileInput.value = "";
            if (list) list.innerHTML = "";
            clearMsg();
        }
    }

    cb.addEventListener('change', () => setEnabled(cb.checked));
    setEnabled(cb.checked);

    if (list) {
        fileInput.addEventListener('change', () => {
            list.innerHTML = "";
            for (const f of fileInput.files) {
                const li = document.createElement("li");
                li.textContent = `${f.name} (${Math.round(f.size / 1024)} KB)`;
                list.appendChild(li);
            }
        });
    }

    btnSave.addEventListener('click', async () => {
        const warningId = getWarningId();
        if (!warningId) {
            alert("WarningId is missing.");
            return;
        }

        const fd = new FormData();
        fd.append("WarningId", warningId);
        fd.append("HasPreviousOffences", cb.checked ? "true" : "false");

        if (cb.checked && fileInput.files && fileInput.files.length > 0) {
            for (const f of fileInput.files) {
                fd.append("Files", f);
            }
        }

        try {
            showMsg("Saving...", false);

            const res = await fetch(form.action, {
                method: "POST",
                body: fd
            });

            const data = await res.json().catch(() => ({}));

            if (!res.ok || data.success === false) {
                showMsg(data.message || "Save failed.", true);
                return;
            }

            showMsg("Saved ✅", false);

            setTimeout(() => {
                const nextStep = document.querySelector('.ws-step[data-step="6"]');
                if (nextStep) {
                    nextStep.click();
                } else {
                    console.error("Step 6 was not found in the stepper.");
                }
            }, 500);
        } catch (e) {
            showMsg("Network error while saving.", true);
        }
    });
    form.addEventListener("submit", (e) => {
        e.preventDefault();
        btnSave.click();
    });
})();