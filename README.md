# DocWatcher

DocWatcher e' un'app desktop per tenere traccia di documenti con data di scadenza e, opzionalmente, un file associato.

## Funzionalita'

- Gestione documenti (crea, modifica, elimina)
- Filtri per scadenza (in scadenza / scaduti / tutti)
- Import/export CSV
- Notifiche Windows con range configurabile
- Avvio in background all'accensione (opzionale)

## Requisiti

- .NET 9 SDK
- Windows 10/11

## Avvio

Apri la soluzione e avvia `DocWatcher.Wpf`. All'avvio il database viene creato/aggiornato in automatico applicando le migrazioni EF Core.

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

## Architettura

- `DocWatcher.Core`: dominio, accesso dati (EF Core + SQLite) e logica CSV. Riutilizzabile e coperto da test.
- `DocWatcher.Wpf`: interfaccia WPF (Windows).
- Dependency Injection tramite Generic Host; i `DbContext` sono creati on-demand da un `IDbContextFactory` (uno per operazione).
- Lo schema del database e' gestito con le migrazioni EF Core (cartella `DocWatcher.Core/Data/Migrations`).
- Versioni dei pacchetti NuGet centralizzate in `Directory.Packages.props` (Central Package Management).

### Migrazioni database

```
dotnet ef migrations add <Nome> --project DocWatcher.Core
```

> Nota: le versioni precedenti creavano il DB con `EnsureCreated()` (senza storico migrazioni).
> Un database creato con quelle versioni va eliminato alla prima esecuzione della nuova versione
> per consentire l'applicazione pulita delle migrazioni.

## Test

```
dotnet test
```

## Rilascio e aggiornamenti automatici

La distribuzione e gli aggiornamenti sono gestiti tramite **GitHub Actions** + **Velopack**.

- **CI** (`.github/workflows/ci.yml`): a ogni push/PR su `main` compila ed esegue i test su Windows.
- **Release** (`.github/workflows/release.yml`): al push di un tag `vX.Y.Z` builda, testa,
  pubblica l'eseguibile, crea l'installer e i pacchetti delta con Velopack e li carica come
  **GitHub Release**.

Per pubblicare una nuova versione basta creare e pushare un tag (la versione viene dedotta dal tag):

```
git tag v1.1.0
git push origin v1.1.0
```

L'app, all'avvio, controlla le GitHub Releases: se c'e' una versione piu' recente la scarica in
background e la applica al riavvio successivo (vedi `App.CheckForUpdatesAsync`). Il controllo e'
un no-op quando l'app viene eseguita da sorgente (non installata via Velopack).

> Il numero `<Version>` nel `.csproj` e' solo il default di sviluppo: nelle release la versione
> proviene dal tag git.
