(() => {
    const dashboard = document.querySelector(".dashboard-shell[data-metrics-url]");
    if (!dashboard) {
        return;
    }

    const feedTitle = document.getElementById("feedTitle");
    const feedDescription = document.getElementById("feedDescription");
    const body = document.getElementById("readingsBody");
    const emptyState = document.getElementById("emptyState");

    function addCell(row, value, className) {
        const cell = document.createElement("td");
        if (className) {
            cell.className = className;
        }
        cell.textContent = value;
        row.appendChild(cell);
    }

    function renderReadings(readings) {
        body.replaceChildren();
        emptyState.hidden = readings.length !== 0;
        for (const reading of readings) {
            const row = document.createElement("tr");
            addCell(row, reading.deviceId, "order-id");
            addCell(row, `${Number(reading.soilMoisturePercent).toFixed(1)}%`);
            addCell(row, `${Number(reading.temperatureC).toFixed(1)} °C`);
            addCell(row, `${Number(reading.humidityPercent).toFixed(1)}%`);
            addCell(row, reading.irrigationState);
            addCell(row, reading.timestamp ? new Date(reading.timestamp).toISOString().replace("T", " ").slice(0, 19) : "—");
            body.appendChild(row);
        }
    }

    async function refreshTelemetry() {
        try {
            const response = await fetch(dashboard.dataset.metricsUrl, {
                headers: { Accept: "application/json" },
                cache: "no-store"
            });
            if (!response.ok) {
                throw new Error(`Telemetry request failed (${response.status}).`);
            }

            const readings = await response.json();
            renderReadings(readings);
            feedTitle.textContent = "Azure Functions connected";
            feedDescription.textContent = `Updated ${new Date().toLocaleTimeString()}`;
        } catch (error) {
            console.error("Unable to refresh farm telemetry.", error);
            feedTitle.textContent = "Telemetry unavailable";
            feedDescription.textContent = "Check Azure Function and storage configuration";
        } finally {
            window.setTimeout(refreshTelemetry, 5000);
        }
    }

    refreshTelemetry();
})();
