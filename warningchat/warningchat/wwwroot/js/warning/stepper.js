(function () {
    const stepper = document.getElementById('wsStepper');
    if (!stepper) return;

    const steps = stepper.querySelectorAll('.ws-step');
    const panes = document.querySelectorAll('.ws-pane');

    function getWarningId() {
        return parseInt(document.getElementById("WarningId")?.value || "0", 10);
    }

    async function loadOverviewPane(warningId) {
        const res = await fetch(`/Transgression/GetOverview?warningId=${warningId}`);

        if (!res.ok) {
            throw new Error("Failed to load overview");
        }

        const html = await res.text();

        const overviewPane = document.querySelector('.ws-pane[data-pane="6"]');

        if (overviewPane) {
            overviewPane.innerHTML = html;
        }
    }

    async function setActive(stepNo) {
        const step = String(stepNo);

        if (step === "6") {
            const warningId = getWarningId();

            if (warningId > 0) {
                try {
                    await loadOverviewPane(warningId);
                } catch (err) {
                    console.error("Failed to refresh overview:", err);
                }
            }
        }

        steps.forEach(s => {
            s.classList.toggle('active', s.dataset.step === step);
        });

        panes.forEach(p => {
            p.classList.toggle('active', p.dataset.pane === step);
        });

        const badge = document.querySelector('.ws-left-badge');
        const title = document.querySelector('.ws-left-title');

        if (badge) {
            badge.textContent = step;
        }

        const label = stepper.querySelector(`.ws-step[data-step="${step}"] .ws-step-label`);

        if (title) {
            title.textContent = label ? label.textContent : "Wizard";
        }
    }

    steps.forEach(s => {
        s.addEventListener('click', async () => {
            await setActive(s.dataset.step);
        });
    });
})();