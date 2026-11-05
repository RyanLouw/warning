(function () {
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
    const d = new Date(
        startDate.getFullYear(),
        startDate.getMonth(),
        startDate.getDate()
    );

    let added = 0;

    while (added < workingDaysToAdd) {
        d.setDate(d.getDate() + 1);

        const day = d.getDay();
        const isWeekend = day === 0 || day === 6;
        if (!isWeekend ) {
            added++;
        }
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

    function getWarningId(btn) {
        const buttonValue = btn?.dataset?.warningId;
        const queryValue = new URL(window.location.href)
            .searchParams.get("id");
        const id = Number.parseInt(buttonValue || queryValue || "0", 10);
        return Number.isInteger(id) && id > 0 ? id : 0;
    }

    async function markAsNew(btn) {
        const missing = getMissingRequired();
        if (missing.length > 0) {
            btn.disabled = true;
            showMsg("Please complete: " + missing.join(", "), true);
            return;
        }

        const warningId = getWarningId(btn);
        if (!warningId) {
            showMsg("WarningId not found in URL.", true);
            return;
        }

        const due = addWorkingDays(new Date(), 2);
        const dto = {
            warningId: warningId,
            status: "New",
            dueDate: toIsoDateOnly(due)
        };

        const oldText = btn.textContent;

        try {
            btn.disabled = true;
            btn.textContent = "Saving...";
            window.appLoader.show();

            await postJson(saveUrl, dto);

            window.location.assign("/");
        } catch (e) {
            window.appLoader.hide();
            console.error(e);
            showMsg(e.message || "Save failed.", true);
            btn.disabled = false;
            btn.textContent = oldText;
        }
    }

    function updateButtonState() {
        const btn = document.getElementById("btnMarkNew");
        if (!btn) return;

        btn.disabled = getMissingRequired().length > 0;
    }

    document.addEventListener("click", function (event) {
        const btn = event.target.closest("#btnMarkNew");
        if (!btn) return;

        event.preventDefault();
        markAsNew(btn);
    });

    document.addEventListener("warning:overview-updated", updateButtonState);
    updateButtonState();
})();
