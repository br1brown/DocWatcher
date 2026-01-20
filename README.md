# DocWatcher

DocWatcher e' un'app desktop per tenere traccia di documenti con data di scadenza e, opzionalmente, un file associato.

## Funzionalita'

- Gestione documenti (crea, modifica, elimina)
- Filtri per scadenza (in scadenza / scaduti / tutti)
- Import/export CSV
- Notifiche Windows con range configurabile
- Avvio in background all'accensione (opzionale)

## Requisiti

- .NET (versione del progetto, es. .NET 8)
- Windows 10/11

## Avvio

Apri la soluzione e avvia `DocWatcher.Wpf`. Alla prima esecuzione il database viene creato in automatico.

## Percorsi e dati

- Database SQLite: `%LOCALAPPDATA%\\DocWatcher\\Data\\docwatcher.db`
- Config: `%LOCALAPPDATA%\\docwatcher.config.json`
- Log: `%LOCALAPPDATA%\\DocWatcher\\log-YYYYMMDD.txt` (pulizia automatica > 7 giorni all'avvio)

## CSV

- Separatore supportato: `;` o `,`
- Campi: `Titolo`, `DataScadenza`, `PercorsoAllegato` (opzionale)
- Formati data: `dd/MM/yyyy`, `d/M/yyyy`, `yyyy-MM-dd`

## Note

- Il filtro della griglia e le notifiche hanno range separati (configurabili da Impostazioni).
- Il manuale utente e' incluso come risorsa nella UI (tab "Guida").
