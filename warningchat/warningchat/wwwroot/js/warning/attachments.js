(function () {
    function getWarningId() {
        const formValue = document.querySelector(
            '#frmSaveAttachment input[name="WarningId"]'
        )?.value;

        const queryValue = new URL(window.location.href)
            .searchParams.get("id");

        return parseInt(formValue || queryValue || "0", 10) || 0;
    }

    const warningId = getWarningId();
    console.log("FINAL WARNING ID:", warningId);

    function getInput() {
        return document.getElementById('attachmentFile');
    }

    function getPending() {
        return document.getElementById('pendingPreview');
    }

    function getPendingBody() {
        return document.getElementById('pendingBody');
    }

    function getContinueButton() {
        return document.getElementById('btnSaveAttachmentsContinue');
    }

    function getForm() {
        return document.getElementById('frmSaveAttachment');
    }

    function getAttachmentNameInput() {
        return document.getElementById('attachmentName');
    }

    function getExistingAttachmentCount() {
        return document.querySelectorAll(".ws-attach-item").length;
    }

    function getSelectedFileCount() {
        const input = getInput();
        return input && input.files ? input.files.length : 0;
    }

    function hasAnyAttachment() {
        return getExistingAttachmentCount() > 0 || getSelectedFileCount() > 0;
    }

    function updateContinueButtonState() {
        const btnContinue = getContinueButton();
        if (!btnContinue) return;

        const canContinue = hasAnyAttachment();

        btnContinue.disabled = !canContinue;
        btnContinue.setAttribute("aria-disabled", String(!canContinue));

        if (!canContinue) {
            btnContinue.classList.add("disabled");
            btnContinue.title = "Please upload at least one attachment before continuing.";
        } else {
            btnContinue.classList.remove("disabled");
            btnContinue.title = "";
        }

        console.log("Attachment button state:", {
            existing: getExistingAttachmentCount(),
            selected: getSelectedFileCount(),
            canContinue
        });
    }

    document.addEventListener("change", function (e) {
        const input = e.target.closest("#attachmentFile");
        if (!input) return;

        const pending = getPending();
        const body = getPendingBody();

        if (!pending || !body) {
            updateContinueButtonState();
            return;
        }

        body.innerHTML = "";

        const existingEmptyText = document.querySelector(".ws-no-attachments-text, .ws-attachments-panel > .text-muted");

        const files = input.files;
        console.log("FILES SELECTED:", files ? files.length : 0);

        if (!files || files.length === 0) {
            pending.classList.add("d-none");
            updateContinueButtonState();
            return;
        }

        if (existingEmptyText) {
            existingEmptyText.remove();
        }

        pending.classList.remove("d-none");

        for (const f of files) {
            const wrapper = document.createElement("div");
            wrapper.className = "mb-2";

            if (f.type && f.type.startsWith("image/")) {
                const img = document.createElement("img");
                img.className = "ws-pending-img";
                img.src = URL.createObjectURL(f);
                wrapper.appendChild(img);
            }

            const meta = document.createElement("div");
            meta.className = "ws-pending-meta";
            meta.textContent = `${f.name} (${Math.round(f.size / 1024)} KB)`;
            wrapper.appendChild(meta);

            body.appendChild(wrapper);
        }

        updateContinueButtonState();

        const btnContinue = getContinueButton();

        if (btnContinue && files.length > 0) {
            btnContinue.disabled = false;
            btnContinue.setAttribute("aria-disabled", "false");
            btnContinue.classList.remove("disabled");
            btnContinue.title = "";
        }
    });

    document.addEventListener("click", async function (e) {
        const removeBtn = e.target.closest(".ws-attach-remove");
        if (!removeBtn) return;

        const fileName = removeBtn.dataset.fileName;

        if (!warningId || !fileName) {
            alert("Missing data");
            return;
        }

        const url = `/Transgression/DeleteAttachment?warningId=${encodeURIComponent(warningId)}&fileName=${encodeURIComponent(fileName)}`;

        try {
            const res = await fetch(url, { method: "GET" });

            if (!res.ok) {
                alert("Delete failed");
                return;
            }

            const json = await res.json();

            if (json?.success) {
                removeBtn.closest(".ws-attach-item")?.remove();

                const list = document.querySelector(".ws-attach-list");

                if (list && list.querySelectorAll(".ws-attach-item").length === 0) {
                    list.remove();

                    const panel = document.querySelector(".ws-attachments-panel");
                    const alreadyHasEmpty = panel?.querySelector(".ws-no-attachments-text");

                    if (panel && !alreadyHasEmpty) {
                        const empty = document.createElement("div");
                        empty.className = "text-muted ws-no-attachments-text";
                        empty.textContent = "No attachments uploaded yet.";
                        panel.appendChild(empty);
                    }
                }

                updateContinueButtonState();
            }
        } catch (err) {
            console.error("DELETE ERROR:", err);
            alert("Delete error");
        }
    });

    async function uploadAttachments() {
        const form = getForm();
        const input = getInput();

        if (!form) {
            console.error("Attachment form could not be found.");

            return {
                success: false,
                message: "The attachment form could not be found."
            };
        }

        if (!input || !input.files || input.files.length === 0) {
            console.log("No files selected, skipping upload.");

            return {
                success: true,
                skipped: true
            };
        }

        const formData = new FormData(form);

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: formData,
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            const contentType =
                response.headers.get("content-type") ?? "";

            let data = null;

            if (contentType.includes("application/json")) {
                data = await response.json();
            } else {
                const responseText = await response.text();

                console.error(
                    "Upload returned a non-JSON response:",
                    response.status,
                    responseText
                );
            }

            if (!response.ok) {
                return {
                    success: false,
                    message:
                        data?.message ??
                        `Upload failed with status ${response.status}.`
                };
            }

            if (data?.success === false) {
                return {
                    success: false,
                    message:
                        data.message ??
                        "The server reported that the upload failed."
                };
            }

            return {
                success: true,
                data
            };
        } catch (error) {
            console.error("Upload request failed:", error);

            return {
                success: false,
                message: "A network error occurred while uploading the file."
            };
        }
    }
    document.addEventListener("click", async function (e) {
        const btnContinue = e.target.closest(
            "#btnSaveAttachmentsContinue"
        );

        if (!btnContinue) {
            return;
        }

        e.preventDefault();
        e.stopPropagation();

        if (!hasAnyAttachment()) {
            alert(
                "Please upload at least one attachment before continuing."
            );

            updateContinueButtonState();
            return;
        }

        const originalButtonText =
            btnContinue.textContent;

        try {
            btnContinue.disabled = true;
            btnContinue.setAttribute(
                "aria-disabled",
                "true"
            );

            btnContinue.textContent = "Saving...";

            const result = await uploadAttachments();

            if (!result.success) {
                return;
            }

            console.log(
                "Attachments saved. Reloading and opening step 7."
            );

            sessionStorage.setItem(
                "warningWizardActiveStep",
                "6"
            );

            window.location.reload();
        } catch (error) {
            console.error(
                "Save attachments and continue error:",
                error
            );

            alert(
                "The attachments could not be saved."
            );

            btnContinue.disabled = false;
            btnContinue.setAttribute(
                "aria-disabled",
                "false"
            );

            btnContinue.textContent =
                originalButtonText;

            updateContinueButtonState();
        }
    });

    document.addEventListener("submit", async function (e) {
        const form = e.target.closest("#frmSaveAttachment");
        if (!form) return;

        e.preventDefault();

        if (!hasAnyAttachment()) {
            alert("Please upload at least one attachment before continuing.");
            updateContinueButtonState();
            return;
        }

        const result = await uploadAttachments();
        if (!result.success) return;

        window.location.reload();
    });

    function runAttachmentButtonChecks() {
        updateContinueButtonState();
        setTimeout(updateContinueButtonState, 100);
        setTimeout(updateContinueButtonState, 300);
        setTimeout(updateContinueButtonState, 700);
        setTimeout(updateContinueButtonState, 1200);
    }

    document.addEventListener("click", function (e) {
        if (e.target.closest(".ws-btn-upload")) {
            setTimeout(updateContinueButtonState, 300);
            setTimeout(updateContinueButtonState, 700);
        }
    });

    runAttachmentButtonChecks();
})();
