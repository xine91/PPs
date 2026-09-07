/**
 * @typedef {{ title?: string, message?: string, severityCode?: number }} LeitstandAlert
 * @typedef {{ snapshotTimeUTC?: string, cpuUsagePercent?: number, memoryUsagePercent?: number, diskUsagePercent?: number, tfaCountToday?: number, tfaActiveUsersWeek?: number, tfaTFLogErrorsToday?: number }} LeitstandHistoryEntry
 * @typedef {{
 *   orgName?: string,
 *   machineName?: string,
 *   snapshotTimeUTC?: string,
 *   ipAddress?: string,
 *   cpuName?: string,
 *   cpuCores?: number,
 *   cpuLogicalProcessors?: number,
 *   cpuUsagePercent?: number,
 *   memoryTotalMB?: number,
 *   memoryFreeMB?: number,
 *   memoryUsagePercent?: number,
 *   diskTotalGB?: number,
 *   diskFreeGB?: number,
 *   diskUsagePercent?: number,
 *   tfaCountToday?: number,
 *   tfaActiveUsersWeek?: number,
 *   tfaTotalDocs?: number,
 *   tfaDocsLast30Days?: number,
 *   tfaTotalFiles?: number,
 *   tfaTFLogErrorsToday?: number,
 *   tf_M365_Mails_today?: number,
 *   tfaLicenseCount?: number,
 *   tfaDatabaseSizeMB?: number,
 *   tfaFileSizeTodayMB?: number,
 *   tfaSQLServerVersion?: string,
 *   tfaDBVersion?: string,
 *   tfaJobServerVersion?: string,
 *   tfaOcrServerVersion?: string,
 *   tfaImageServerVersion?: string,
 *   tfaMyWorkVersion?: string,
 *   tfaAdministrationVersion?: string,
 *   osVersion?: string,
 *   osArchitecture?: string,
 *   dotNetVersion?: string,
 *   systemUptimeHours?: number,
 *   tfaLicenseEnd?: string,
 *   tfaLastBackupDateArchiv?: string,
 *   tfaLastBackupDateTopfact6?: string,
 *   processTotal?: number,
 *   processResponding?: number,
 *   processNotResponding?: number,
 *   processTopCpu?: string,
 *   processTopMemory?: string
 * }} LeitstandSnapshot
 * @typedef {{ snapshot?: LeitstandSnapshot, history?: LeitstandHistoryEntry[], alerts?: LeitstandAlert[] }} LeitstandResponse
 */

const detail = document.getElementById('orgDetail');
const tenantInput = document.getElementById('tenantSearchInput');

const emptyPlaceholderHtml = '<div class="detail-placeholder"><div class="text-center"><i class="ri-cursor-line" style="font-size:2.5rem; display:block; margin-bottom:0.5rem;"></i>Bitte wählen Sie einen Tenant aus der Liste aus.</div></div>';
const errorPlaceholderHtml = '<div class="detail-placeholder"><div class="text-center text-danger"><i class="ri-error-warning-line" style="font-size:2rem;display:block;margin-bottom:0.5rem;"></i>Fehler beim Laden der Daten.</div></div>';
const loadingHtml = '<div class="spinner-wrap"><div class="spinner-border" role="status"><span class="visually-hidden">Laden...</span></div></div>';

const usageColor = pct => {
    if (pct == null) {
        return '#4b5563';
    }

    if (pct < 60) {
        return '#10b981';
    }

    if (pct < 85) {
        return '#f59e0b';
    }

    return '#ef4444';
};

const formatValue = (value, suffix = '') => {
    if (value === null || value === undefined) {
        return '–';
    }

    if (typeof value === 'number' && Number.isFinite(value)) {
        return value.toLocaleString('de-DE') + suffix;
    }

    return `${value}${suffix}`;
};



const alertSeverityClass = severityCode => severityCode === 2 ? 'critical' : 'warning';
const alertSeverityLabel = severityCode => severityCode === 2 ? 'Rot' : 'Gelb';

const renderAlerts = alerts => {
    if (!alerts?.length) {
        return `
            <div class="alerts-section">
                <div class="alerts-header">
                    <h5><i class="ri-alarm-warning-line"></i> Aktuelle Meldungen</h5>
                    <span class="alerts-count">0 Einträge</span>
                </div>
                <div class="alerts-empty">
                    <i class="ri-checkbox-circle-line"></i>
                    <span>Aktuell keine gelben oder roten Probleme.</span>
                </div>
            </div>`;
    }

    const items = alerts.map(alert => {
        const severityClass = alertSeverityClass(alert.severityCode);
        const severityLabel = alertSeverityLabel(alert.severityCode);

        return `
            <div class="alert-item alert-item-${severityClass}">
                <div class="alert-item-header">
                    <div class="alert-title">${formatValue(alert.title)}</div>
                    <span class="alert-badge alert-badge-${severityClass}">${severityLabel}</span>
                </div>
                <div class="alert-message">${formatValue(alert.message)}</div>
            </div>`;
    }).join('');

    return `
        <div class="alerts-section">
            <div class="alerts-header">
                <h5><i class="ri-alarm-warning-line"></i> Aktuelle Meldungen</h5>
                <span class="alerts-count">${alerts.length} Einträge</span>
            </div>
            <div class="alerts-list">${items}</div>
        </div>`;
};

/**
 * @param {LeitstandResponse} data
 */
const renderDetail = data => {
    if (!detail || !data.snapshot) {
        return;
    }

    const snapshot = data.snapshot;
    const history = data.history ?? [];
    const alerts = data.alerts ?? [];
    const memoryUsed = snapshot.memoryTotalMB && snapshot.memoryFreeMB
        ? `${((snapshot.memoryTotalMB - snapshot.memoryFreeMB) / 1024).toFixed(1)} GB`
        : '';
    const memoryTotal = snapshot.memoryTotalMB
        ? `${(snapshot.memoryTotalMB / 1024).toFixed(1)} GB`
        : '';
    const diskUsed = snapshot.diskTotalGB && snapshot.diskFreeGB
        ? `${snapshot.diskTotalGB - snapshot.diskFreeGB} GB`
        : '';
    const diskTotal = snapshot.diskTotalGB ? `${snapshot.diskTotalGB} GB` : '';

    let html = `
        <div class="detail-header">
            <div>
                <h2><i class="ri-building-4-line" style="margin-right:0.5rem;"></i>${formatValue(snapshot.orgName)}</h2>
                <div class="meta"><i class="ri-server-line"></i> ${formatValue(snapshot.machineName)} &bull; ${formatValue(snapshot.ipAddress)}</div>
            </div>
            <div class="meta text-end">
                Letzter Snapshot<br><strong>${formatValue(snapshot.snapshotTimeUTC)}</strong>
            </div>
        </div>

        <div class="stat-row">
            <div class="stat-card" style="--kpi-color:#22c55e;">
                <span class="stat-label">Aktive User (Woche)</span>
                <span class="stat-value">${formatValue(snapshot.tfaActiveUsersWeek)}</span>
                <span class="stat-sub">Aktive Logins 7 Tage</span>
            </div>
            <div class="stat-card" style="--kpi-color:#3b82f6;">
                <span class="stat-label">TFA Aufrufe heute</span>
                <span class="stat-value">${formatValue(snapshot.tfaCountToday)}</span>
                <span class="stat-sub">Heutige Aufrufe</span>
            </div>
            <div class="stat-card" style="--kpi-color:#6366f1;">
                <span class="stat-label">Dokumente gesamt</span>
                <span class="stat-value">${formatValue(snapshot.tfaTotalDocs)}</span>
                <span class="stat-sub">Gesamter Bestand</span>
            </div>
            <div class="stat-card" style="--kpi-color:#06b6d4;">
                <span class="stat-label">Dokumente (30 Tage)</span>
                <span class="stat-value">${formatValue(snapshot.tfaDocsLast30Days)}</span>
                <span class="stat-sub">Letzte 30 Tage</span>
            </div>
            <div class="stat-card" style="--kpi-color:#8b5cf6;">
                <span class="stat-label">Dateien gesamt</span>
                <span class="stat-value">${formatValue(snapshot.tfaTotalFiles)}</span>
                <span class="stat-sub">Datei-Anzahl gesamt</span>
            </div>
            <div class="stat-card" style="--kpi-color:${snapshot.tfaTFLogErrorsToday > 0 ? '#ef4444' : '#22c55e'};">
                <span class="stat-label">Fehler heute</span>
                <span class="stat-value">${formatValue(snapshot.tfaTFLogErrorsToday)}</span>
                <span class="stat-sub">TF-Log Einträge</span>
            </div>
            <div class="stat-card" style="--kpi-color:#a78bfa;">
                <span class="stat-label">M365 Mails heute</span>
                <span class="stat-value">${formatValue(snapshot.tf_M365_Mails_today)}</span>
                <span class="stat-sub">Eingehende Mails</span>
            </div>
            <div class="stat-card" style="--kpi-color:#14b8a6;">
                <span class="stat-label">Lizenzen</span>
                <span class="stat-value">${formatValue(snapshot.tfaLicenseCount)}</span>
                <span class="stat-sub">Aktive Lizenzen</span>
            </div>
        </div>

        ${renderAlerts(alerts)}

        <div class="stat-row">
            ${usageBar('CPU-Auslastung', snapshot.cpuUsagePercent)}
            ${usageBar('RAM-Auslastung', snapshot.memoryUsagePercent, memoryUsed, memoryTotal)}
            ${usageBar('Disk-Auslastung', snapshot.diskUsagePercent, diskUsed, diskTotal)}
        </div>

        <div class="info-grid">
            <div class="info-card">
                <h5><i class="ri-cpu-line"></i> CPU</h5>
                <div class="info-row"><span class="label">Prozessor</span><span class="value">${formatValue(snapshot.cpuName)}</span></div>
                <div class="info-row"><span class="label">Kerne / Logisch</span><span class="value">${formatValue(snapshot.cpuCores)} / ${formatValue(snapshot.cpuLogicalProcessors)}</span></div>
            </div>
            <div class="info-card">
                <h5><i class="ri-ram-line"></i> Arbeitsspeicher</h5>
                <div class="info-row"><span class="label">Gesamt</span><span class="value">${memoryTotal || '–'}</span></div>
                <div class="info-row"><span class="label">Genutzt</span><span class="value">${memoryUsed || '–'}</span></div>
            </div>
            <div class="info-card">
                <h5><i class="ri-hard-drive-3-line"></i> Festplatte</h5>
                <div class="info-row"><span class="label">Gesamt</span><span class="value">${diskTotal || '–'}</span></div>
                <div class="info-row"><span class="label">Genutzt</span><span class="value">${diskUsed || '–'}</span></div>
            </div>
            <div class="info-card">
                <h5><i class="ri-database-2-line"></i> Datenbank</h5>
                <div class="info-row"><span class="label">DB-Größe</span><span class="value">${snapshot.tfaDatabaseSizeMB ? `${(snapshot.tfaDatabaseSizeMB / 1024).toFixed(1)} GB` : '–'}</span></div>
                <div class="info-row"><span class="label">Dateigröße heute</span><span class="value">${formatValue(snapshot.tfaFileSizeTodayMB, ' MB')}</span></div>
                <div class="info-row"><span class="label">SQL Server</span><span class="value" style="font-size:0.78rem;">${formatValue(snapshot.tfaSQLServerVersion)}</span></div>
            </div>
        </div>

        <div class="info-grid">
            <div class="info-card">
                <h5><i class="ri-git-branch-line"></i> Versionen</h5>
                <div class="info-row"><span class="label">DB-Version</span><span class="value">${formatValue(snapshot.tfaDBVersion)}</span></div>
                <div class="info-row"><span class="label">JobServer</span><span class="value">${formatValue(snapshot.tfaJobServerVersion)}</span></div>
                <div class="info-row"><span class="label">OCR Server</span><span class="value">${formatValue(snapshot.tfaOcrServerVersion)}</span></div>
                <div class="info-row"><span class="label">Image Server</span><span class="value">${formatValue(snapshot.tfaImageServerVersion)}</span></div>
                <div class="info-row"><span class="label">MyWork</span><span class="value">${formatValue(snapshot.tfaMyWorkVersion)}</span></div>
                <div class="info-row"><span class="label">Administration</span><span class="value">${formatValue(snapshot.tfaAdministrationVersion)}</span></div>
            </div>
            <div class="info-card">
                <h5><i class="ri-computer-line"></i> System</h5>
                <div class="info-row"><span class="label">OS</span><span class="value" style="font-size:0.78rem;">${formatValue(snapshot.osVersion)}</span></div>
                <div class="info-row"><span class="label">Architektur</span><span class="value">${formatValue(snapshot.osArchitecture)}</span></div>
                <div class="info-row"><span class="label">.NET</span><span class="value">${formatValue(snapshot.dotNetVersion)}</span></div>
                <div class="info-row"><span class="label">Uptime</span><span class="value">${snapshot.systemUptimeHours ? `${Math.floor(snapshot.systemUptimeHours / 24)} Tage ${snapshot.systemUptimeHours % 24} Std.` : '–'}</span></div>
                <div class="info-row"><span class="label">Lizenz bis</span><span class="value">${formatValue(snapshot.tfaLicenseEnd)}</span></div>
            </div>
        </div>

        <div class="info-grid">
            <div class="info-card">
                <h5><i class="ri-shield-check-line"></i> Backup</h5>
                <div class="info-row"><span class="label">Archiv</span><span class="value">${formatValue(snapshot.tfaLastBackupDateArchiv)}</span></div>
                <div class="info-row"><span class="label">topfact6</span><span class="value">${formatValue(snapshot.tfaLastBackupDateTopfact6)}</span></div>
            </div>
            <div class="info-card">
                <h5><i class="ri-settings-3-line"></i> Prozesse</h5>
                <div class="info-row"><span class="label">Gesamt</span><span class="value">${formatValue(snapshot.processTotal)}</span></div>
                <div class="info-row"><span class="label">Aktiv</span><span class="value" style="color:#10b981;">${formatValue(snapshot.processResponding)}</span></div>
                <div class="info-row"><span class="label">Nicht reagierend</span><span class="value" style="color:${snapshot.processNotResponding > 0 ? '#ef4444' : '#10b981'};">${formatValue(snapshot.processNotResponding)}</span></div>
                <div class="info-row"><span class="label">Top CPU</span><span class="value">${formatValue(snapshot.processTopCpu)}</span></div>
                <div class="info-row"><span class="label">Top Memory</span><span class="value">${formatValue(snapshot.processTopMemory)}</span></div>
            </div>
        </div>`;

    if (history.length > 0) {
        const historyRows = history.map(row => `
            <tr>
                <td>${formatValue(row.snapshotTimeUTC)}</td>
                <td style="color:${usageColor(row.cpuUsagePercent)}">${row.cpuUsagePercent != null ? `${row.cpuUsagePercent.toFixed(1)}%` : '–'}</td>
                <td style="color:${usageColor(row.memoryUsagePercent)}">${row.memoryUsagePercent != null ? `${row.memoryUsagePercent.toFixed(1)}%` : '–'}</td>
                <td style="color:${usageColor(row.diskUsagePercent)}">${row.diskUsagePercent != null ? `${row.diskUsagePercent.toFixed(1)}%` : '–'}</td>
                <td>${formatValue(row.tfaCountToday)}</td>
                <td>${formatValue(row.tfaActiveUsersWeek)}</td>
                <td style="color:${row.tfaTFLogErrorsToday > 0 ? '#ef4444' : '#10b981'}">${formatValue(row.tfaTFLogErrorsToday)}</td>
            </tr>`).join('');

        html += `
            <div class="history-section">
                <h5><i class="ri-history-line"></i> Snapshot-Verlauf (letzte ${history.length})</h5>
                <div style="overflow-x:auto;">
                    <table class="history-table">
                        <thead>
                            <tr>
                                <th>Zeitpunkt</th><th>CPU %</th><th>RAM %</th><th>Disk %</th>
                                <th>TFA Aufrufe</th><th>Aktive User</th><th>Fehler</th>
                            </tr>
                        </thead>
                        <tbody>${historyRows}</tbody>
                    </table>
                </div>
            </div>`;
    }

    detail.innerHTML = html;
};

const renderEmptyPlaceholder = () => {
    if (detail) {
        detail.innerHTML = emptyPlaceholderHtml;
    }
};

const renderErrorPlaceholder = () => {
    if (detail) {
        detail.innerHTML = errorPlaceholderHtml;
    }
};

const selectOrg = async orgName => {
    if (!detail) {
        return;
    }

    if (!orgName) {
        renderEmptyPlaceholder();
        return;
    }

    const detailUrl = detail.dataset.detailUrl;
    if (!detailUrl) {
        renderErrorPlaceholder();
        return;
    }

    detail.innerHTML = loadingHtml;

    try {
        const response = await fetch(`${detailUrl}?org=${encodeURIComponent(orgName)}`);
        if (!response.ok) {
            throw new Error(`Request failed with status ${response.status}`);
        }

        /** @type {LeitstandResponse} */
        const data = await response.json();
        renderDetail(data);
    }
    catch {
        renderErrorPlaceholder();
    }
};

const initializeLeitstand = () => {
    if (!(tenantInput instanceof HTMLInputElement) || !detail) {
        return;
    }

    if (tenantInput.value) {
        void selectOrg(tenantInput.value);
    }

    tenantInput.addEventListener('change', () => {
        void selectOrg(tenantInput.value);
    });
};

document.addEventListener('DOMContentLoaded', initializeLeitstand);
