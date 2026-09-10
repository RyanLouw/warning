(function () {
    let calendarInitialized = false;

    function initCalendarIfNeeded() {
        if (calendarInitialized) return;

        if (typeof window.jQuery === "undefined") {
            return;
        }

        if (typeof $.fn.datepicker === "undefined") {
            return;
        }

        const $cal = $('#wsCalendar');

        if ($cal.length === 0) {
            return;
        }

        const warningId = parseInt($("#WarningId").val() || "0", 10);

        let initialDates = [];
        const existingJson = $("#Q1_AnswerJson").val();
        if (existingJson) {
            try {
                const obj = JSON.parse(existingJson);
                if (obj && Array.isArray(obj.dates)) initialDates = obj.dates;
            } catch { }
        }
        $cal.datepicker({
            format: "yyyy-mm-dd",
            todayHighlight: true,
            autoclose: false,
            multidate: true,
            endDate: new Date()
        });

        if (initialDates.length > 0) {
            $cal.datepicker('setDates', initialDates);
            $("#wsSelectedCount").text(initialDates.length);
        }
        function showSaveMessage(text, isError) {
            const el = document.getElementById("wsSaveMsg");
            if (!el) return;

            el.style.display = "block";
            el.className = isError
                ? "alert alert-danger mt-2"
                : "alert alert-success mt-2";

            el.textContent = text;

            setTimeout(() => {
                el.style.display = "none";
            }, 3000);
        }
        function syncHiddenFromPicker() {
            const dates = ($cal.datepicker('getDates') || [])
                .map(d => d.toISOString().split('T')[0]);

            $("#Q1_AnswerJson").val(JSON.stringify({ dates }));
            $("#Q1_AnswerText").val(dates.join(", "));
            $("#wsSelectedCount").text(dates.length);
        }

        $cal.on('changeDate', syncHiddenFromPicker);
        syncHiddenFromPicker();

        $("#wsSaveDatesBtn").off("click").on("click", async function () {
            if (!warningId || warningId <= 0) {
                showSaveMessage("Please create the warning first (Step 1).", true);
                return;
            }

            const payload = {
                warningId: warningId,
                questionId: 1,
                answerText: $("#Q1_AnswerText").val(),
                answerJson: $("#Q1_AnswerJson").val()
            };

            try {
                console.log("Posting to:", new URL('/Transgression/SaveAnswer', window.location.origin).toString());
                console.log("Current page:", window.location.href);
                const res = await fetch('/Transgression/SaveAnswer', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });

                const data = await res.json();

                if (!res.ok || !data.success) {
                    showSaveMessage(data.message || "Failed to save.", true);
                    return;
                }

                showSaveMessage("Dates saved successfully ✅", false);

                window.warningWizard?.reloadAtStep(3);
            } catch (e) {
                alert("Network/error while saving.");
            }
        });

        calendarInitialized = true;
    }

    const stepper = document.getElementById('wsStepper');
    if (!stepper) return;

    stepper.querySelectorAll('.ws-step').forEach(s => {
        s.addEventListener('click', () => {
            const stepNo = String(s.dataset.step);
            if (stepNo === "2") {
                setTimeout(initCalendarIfNeeded, 0);
            }
        });
    });
})();
