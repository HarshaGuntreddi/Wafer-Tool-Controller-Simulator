const statusEl = document.getElementById('status');
const telemetryEl = document.getElementById('telemetry');
const startBtn = document.getElementById('startBtn');
const stopBtn = document.getElementById('stopBtn');

async function poll() {
    try {
        const status = await fetch('/api/status').then(r => r.json());
        statusEl.textContent = `Connected: ${status.connected} | State: ${status.toolState}`;
    } catch {
        statusEl.textContent = 'Status unavailable';
    }

    try {
        const telemetry = await fetch('/api/telemetry/latest?limit=10').then(r => r.json());
        renderTelemetry(telemetry);
    } catch {
        /* ignore */
    }
}

function renderTelemetry(items) {
    telemetryEl.innerHTML = '';
    for (const item of items) {
        const li = document.createElement('li');
        li.textContent = `${item.timestamp} ${item.key}: ${item.value}`;
        telemetryEl.appendChild(li);
    }
}

startBtn.addEventListener('click', () => {
    fetch('/api/run/start', { method: 'POST' });
});

stopBtn.addEventListener('click', () => {
    fetch('/api/run/stop', { method: 'POST' });
});

setInterval(poll, 1000);
poll();

if (!!window.EventSource) {
    const es = new EventSource('/api/stream');
    es.onmessage = e => {
        try {
            const data = JSON.parse(e.data);
            if (data.status) {
                statusEl.textContent = `Connected: ${data.status.connected} | State: ${data.status.toolState}`;
            }
            if (data.telemetry) {
                renderTelemetry([data.telemetry]);
            }
        } catch {
            /* ignore */
        }
    };
}
