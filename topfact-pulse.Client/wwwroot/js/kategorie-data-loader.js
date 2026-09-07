/**
 * kategorie-data-loader.js
 * Lädt automatisch Kategorie-Daten auf Seiten mit KategorieID
 * Initialisiert AG Grid und rendert KPI-Indikatoren
 */

(function() {
    'use strict';

    const MODULE_NAME = '[KategorieDataLoader]';
    const LOGGER = {
        log: (msg) => console.log(`${MODULE_NAME} ${msg}`),
        warn: (msg) => console.warn(`${MODULE_NAME} ${msg}`),
        error: (msg) => console.error(`${MODULE_NAME} ${msg}`)
    };

    /**
     * Extrahiert KategorieID aus URL oder Seiten-Kontext
     * Priorität: URL > Data-Attribute > Span-Text-Match > Hidden-Field
     */
    function extractKategorieId() {
        // 1. Aus URL Query-String (HÖCHSTE PRIORITÄT)
        const urlParams = new URLSearchParams(window.location.search);
        const urlId = urlParams.get('kategorieId');
        if (urlId) {
            const id = parseInt(urlId, 10);
            if (id > 0) {
                LOGGER.log(`KategorieID aus URL gefunden: ${id}`);
                return id;
            }
        }

        // 2. Aus Data-Attribute
        const mainElement = document.querySelector('main') || document.body;
        const dataId = mainElement.getAttribute('data-kategorie-id');
        if (dataId) {
            const id = parseInt(dataId, 10);
            if (id > 0) {
                LOGGER.log(`KategorieID aus Data-Attribute gefunden: ${id}`);
                return id;
            }
        }

        // 3. Aus aktiven Navigations-Link (Span Text mit Kategorie-Title abgleichen)
        const activeNavLink = document.querySelector('a.active span, nav .active span, .nav-item.active span');
        if (activeNavLink && activeNavLink.textContent) {
            const categoryTitle = activeNavLink.textContent.trim();
            LOGGER.log(`Versuche Kategorie-Title zu finden: "${categoryTitle}"`);

            // Rufe Backend auf um KategorieID basierend auf Title zu finden
            findKategorieIdByTitle(categoryTitle).then(id => {
                if (id && id > 0) {
                    LOGGER.log(`KategorieID durch Title-Match gefunden: ${id}`);
                    loadAndDisplayKategorieData(id);
                }
            }).catch(err => {
                LOGGER.warn(`Fehler beim Title-Match: ${err.message}`);
            });

            return null; // Asynchrone Verarbeitung
        }

        // 4. Aus Hidden Input Field
        const hiddenField = document.querySelector('input[type="hidden"][name="kategorieId"], input[type="hidden"][id="kategorieId"]');
        if (hiddenField && hiddenField.value) {
            const id = parseInt(hiddenField.value, 10);
            if (id > 0) {
                LOGGER.log(`KategorieID aus Hidden-Field gefunden: ${id}`);
                return id;
            }
        }

        LOGGER.warn('Keine KategorieID gefunden');
        return null;
    }

    /**
     * Findet KategorieID basierend auf Kategorie-Title
     */
    async function findKategorieIdByTitle(title) {
        if (!title || title.length === 0) {
            return null;
        }

        try {
            LOGGER.log(`Suche nach Kategorie mit Title: "${title}"`);

            const response = await fetch(`/Home/GetKategorieIdByTitle?title=${encodeURIComponent(title)}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                LOGGER.error(`HTTP ${response.status} beim Suchen der Kategorie`);
                return null;
            }

            const data = await response.json();
            LOGGER.log(`Suchresultat:`, data);

            if (data.kategorieId && data.kategorieId > 0) {
                return data.kategorieId;
            }

            return null;

        } catch (error) {
            LOGGER.error(`Fehler beim Title-Suchen: ${error.message}`);
            return null;
        }
    }

    /**
     * Initialisiert AG Grid mit vordefinierten Spalten
     */
    function setupAgGridLicensing() {
        // AG Grid Community Edition initialisieren
        if (typeof agGrid !== 'undefined') {
            LOGGER.log('AG Grid Library geladen');
        } else {
            LOGGER.warn('AG Grid Library nicht verfügbar');
        }
    }

    /**
     * Lädt Kategorie-Daten über API und zeigt sie an
     * Unterstützt sowohl SQL- als auch API-Connector-Daten
     */
        async function loadAndDisplayKategorieData(kategorieId, bereich = 'Technik') {
            LOGGER.log(`Lade Daten für KategorieID: ${kategorieId}, Bereich: ${bereich}`);

            try {
                const response = await fetch(`/Home/GetKategorieData?kategorieId=${kategorieId}`, {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    }
                });

                if (!response.ok) {
                    LOGGER.error(`HTTP ${response.status} beim Abrufen der Daten`);
                    return false;
                }

                const apiResponse = await response.json();
                LOGGER.log('API Response Wrapper empfangen:', apiResponse);

                // Entpacke die Wrapper-Struktur: { statusCode, isSuccess, body, ... }
                if (!apiResponse.isSuccess || apiResponse.statusCode !== 200) {
                    LOGGER.error('API Fehler:', {
                        statusCode: apiResponse.statusCode,
                        isSuccess: apiResponse.isSuccess
                    });
                    return false;
                }

                const data = apiResponse.body;
                if (!data) {
                    LOGGER.error('Keine body in API-Response');
                    return false;
                }

                LOGGER.log('Body extrahiert:', {
                    kategorieId: data.kategorieId,
                    kategorieName: data.kategorieName,
                    queryResultsCount: data.queryResults ? data.queryResults.length : 0,
                    kpisCount: data.kpis ? data.kpis.length : 0
                });

                const hasQueryData = data.queryResults && data.queryResults.length > 0;
                const hasKpiData = data.kpis && data.kpis.length > 0;

                if (!hasQueryData && !hasKpiData) {
                    LOGGER.log(`Keine Daten für KategorieID ${kategorieId}`);
                    return false;
                }

                // Connector-Typ Erkennung für jedes Query Result
                if (hasQueryData) {
                    LOGGER.log('======== Connector-Typ Analyse ========');
                    data.queryResults.forEach((qr, idx) => {
                        let connectorType = 'UNBEKANNT';
                        let details = {};

                        try {
                            const datenParsed = typeof qr.daten === 'string' ? JSON.parse(qr.daten) : qr.daten;

                            if (datenParsed && datenParsed.rowCount !== undefined) {
                                connectorType = 'SQL';
                                details = {
                                    rowCount: datenParsed.rowCount,
                                    rowsLength: Array.isArray(datenParsed.rows) ? datenParsed.rows.length : 'KEIN ARRAY'
                                };
                            } else if (datenParsed && datenParsed.statusCode !== undefined) {
                                connectorType = 'API';
                                details = {
                                    statusCode: datenParsed.statusCode,
                                    isSuccess: datenParsed.isSuccess,
                                    hasBody: !!datenParsed.body,
                                    bodyType: datenParsed.body ? (Array.isArray(datenParsed.body) ? 'Array' : typeof datenParsed.body) : 'null'
                                };
                            }
                        } catch (e) {
                            connectorType = 'PARSE-FEHLER';
                            details = { error: e.message };
                        }

                        LOGGER.log(`Query #${idx} (QueryID=${qr.QueryID}): ConnectorTyp=${connectorType}`, details);
                    });
                    LOGGER.log('=======================================');
                }

                // Rufe globale Funktion aus der Partial View auf
                if (typeof window.loadKategorieData === 'function') {
                    LOGGER.log(`Rufe window.loadKategorieData(${kategorieId}, ${bereich}) auf`);
                    window.loadKategorieData(kategorieId, bereich);
                    return true;
                } else {
                    LOGGER.warn('Funktion window.loadKategorieData nicht verfügbar - versuche direktes Rendering');
                    return false;
                }

            } catch (error) {
                LOGGER.error(`Fehler beim Laden: ${error.message}`);
                return false;
            }
        }

    /**
     * Auto-Initialization wenn DOM bereit ist
     */
    function initializeAutoLoad() {
        LOGGER.log('Initialisiere Auto-Load');

        // Setup AG Grid Licensing
        setupAgGridLicensing();

        // Versuche KategorieID zu extrahieren
        const kategorieId = extractKategorieId();

        if (kategorieId && kategorieId > 0) {
            // Warte bis AG Grid Library verfügbar ist
            waitForAgGrid(() => {
                loadAndDisplayKategorieData(kategorieId);
            });
        } else {
            LOGGER.log('Keine gültige KategorieID - Auto-Load übersprungen');
        }
    }

    /**
     * Wartet bis AG Grid verfügbar ist
     */
    function waitForAgGrid(callback, attempts = 0) {
        if (typeof agGrid !== 'undefined') {
            callback();
        } else if (attempts < 50) {
            // Max 5 Sekunden Wartezeit (50 * 100ms)
            setTimeout(() => waitForAgGrid(callback, attempts + 1), 100);
        } else {
            LOGGER.warn('AG Grid nicht verfügbar nach Wartezeit');
            callback(); // Trotzdem callback aufrufen
        }
    }

    /**
     * Öffentliche API für manuelle Steuerung
     */
        window.KategorieDataLoader = {
            load: function(kategorieId, bereich = 'Technik') {
                LOGGER.log(`Manuelle Anforderung zum Laden: ${kategorieId}, Bereich: ${bereich}`);
                if (kategorieId && kategorieId > 0) {
                    loadAndDisplayKategorieData(kategorieId, bereich);
                }
            },
            extractId: extractKategorieId,
            log: LOGGER.log,
            warn: LOGGER.warn,
            error: LOGGER.error
        };

    // Auto-Initialize auf DOM Ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeAutoLoad);
    } else {
        // DOM ist bereits bereit
        initializeAutoLoad();
    }

    LOGGER.log('Modul geladen und initialisiert');
})();
    