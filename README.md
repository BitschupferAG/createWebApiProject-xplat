# createWebApiProject-xplat

Erzeugt mit **einem Befehl** eine fertige ASP.NET-Core-Web-API-Solution (Swagger, CORS, Logging, optional Entity Framework Core) – auf **Windows, macOS und Linux**.

`xplat` steht für *cross-platform*.

## Warum es dieses Tool gibt

Im Unterricht an der HTL Grieskirchen verwenden wir das Skript
[**createWebApiProject**](https://gitlab.com/rgrueneis/createwebapiproject) von **Prof. Robert Grüneis**.
Es nimmt einem viel Routinearbeit ab. Weil es aus Windows-Batch-Dateien und Windows-Programmen (`.exe`) besteht, läuft es aber **nur unter Windows**. Wer mit einem Mac oder unter Linux arbeitet, konnte es bisher nicht verwenden.

Dieses Projekt löst genau dieses Problem: Es erzeugt dieselbe Art von Projekt mit derselben Aufrufsyntax, läuft aber auf allen drei Betriebssystemen.

> **Danke, Prof. Grüneis!** Die Idee, der Funktionsumfang und die Aufrufsyntax stammen aus dem Original. Dieses Repo ist nur eine plattformunabhängige Neuumsetzung dieser Idee. Wer Windows verwendet und das Original kennt, kann gerne weiter das Original nehmen.

Die erzeugten Projekte referenzieren die öffentlichen NuGet-Pakete `GrueneisR.RestClientGenerator` und `GrueneisR.SignalRAnalyzer` von Prof. Grüneis, damit sie zum Unterricht passen. Diese Pakete werden **nicht** mitgeliefert. `dotnet` lädt sie beim Build ganz normal von nuget.org.

## Installation

Einfach einen Befehl ins Terminal kopieren:

**macOS / Linux** (Terminal):

```bash
curl -fsSL https://raw.githubusercontent.com/BitschupferAG/createWebApiProject-xplat/main/install.sh | bash
```

**Windows** (PowerShell):

```powershell
irm https://raw.githubusercontent.com/BitschupferAG/createWebApiProject-xplat/main/install.ps1 | iex
```

Der Installer

1. installiert das **.NET 10 SDK**, falls es fehlt oder zu alt ist (mit Microsofts offiziellem `dotnet-install`-Skript, nach `~/.dotnet`),
2. installiert oder aktualisiert **dotnet-ef 10**,
3. lädt dieses Tool nach `~/.createWebApiProject-xplat`,
4. trägt alles in den **PATH** ein: bei macOS/Linux in `~/.zshrc` bzw. `~/.bashrc`, bei Windows im Benutzer-PATH.

Danach **ein neues Terminal öffnen**, und schon geht's los. Zum Aktualisieren den Installer einfach noch einmal ausführen.

Den Installer muss man nicht verwenden. Wer .NET 10 und dotnet-ef 10 schon hat, kann das Repo auch einfach klonen und den Ordner selbst in den PATH aufnehmen.

## Verwendung

```
createWebApi solutionName [targetFolder] [pathToDbFile] [tableName] [addWPF]
createMinApi solutionName [targetFolder] [pathToDbFile] [tableName] [addWPF]
createAllApi solutionName [targetFolder] [pathToDbFile] [tableName] [addWPF]
```

| Befehl         | erzeugt                                   |
|----------------|-------------------------------------------|
| `createWebApi` | Backend mit Controllern                   |
| `createMinApi` | Backend mit Minimal API                   |
| `createAllApi` | Backend mit Minimal API **und** Controllern |

| Parameter      | optional | Bedeutung |
|----------------|:--------:|-----------|
| `solutionName` |          | Name der Solution, des Projekts und des Ordners |
| `targetFolder` | ✓        | Ordner, in dem die Solution angelegt wird (Standard: `.`) |
| `pathToDbFile` | ✓        | Datenbankdatei, dann wird ein EF-Core-Projekt erzeugt. `.sqlite`/`.db` überall, `.mdf` nur unter Windows. Relative Pfade gelten ab dem aktuellen Ordner. |
| `tableName`    | ✓        | Für diese Tabelle wird ein GET-Endpoint erzeugt. `null` erzeugt ein **Code-First**-Projekt mit der Tabelle `Dummies`. |
| `addWPF`       | ✓        | `true` erzeugt zusätzlich einen WPF-Client (**nur Windows**) |

### Beispiele

```bash
# Backend mit Controllern, ohne Datenbank, im aktuellen Ordner
createWebApi MyBackend .

# Minimal API mit SQLite-Datenbank, Endpoint für die Tabelle Products
createMinApi NorthwindManager ~/Projekte ~/Downloads/Northwnd.sqlite Products

# Code First: neue SQLite-Datenbank Dummy.sqlite mit Tabelle Dummies
createAllApi DummyBackend . Dummy.sqlite null

# Windows: SQL-Server-Datenbank (.mdf) + WPF-Client
createWebApi NorthwindManager C:\Temp D:\Temp\Northwnd.mdf Categories true
```

Ohne Datenbank, aber mit WPF-Client, übergibst du leere Werte: `createWebApi MyBackend . "" "" true`

Am Ende wird die Solution gebaut. Nach **Enter** startet das Backend:

- Swagger: <http://localhost:5000/swagger>
- `/` leitet auf Swagger weiter
- Nach dem ersten Request erzeugt der RestClientGenerator die Datei `_requests.http`.

### ⚠️ macOS: Port 5000 und AirPlay

Auf macOS belegt der **AirPlay-Empfänger** standardmäßig Port 5000, dann kann das Backend nicht starten. Das Tool warnt in diesem Fall. Abhilfe: **Systemeinstellungen → Allgemein → AirDrop & Handoff → AirPlay-Empfänger** ausschalten.

## Was erzeugt wird

```
MyBackend/
├── MyBackend.slnx
├── MyBackend/                    Web-API-Projekt
│   ├── Program.cs                Swagger, CORS (alles erlaubt), Logging, RestClientGenerator
│   ├── Controllers/              (Web/All) ValuesController
│   ├── Maps/                     (Min/All) Minimal-API-Endpoints
│   ├── Dtos/  Services/          leere Ordner mit Platzhalter-Klasse
│   ├── Logging/                  kompakter Konsolen-Logger (AddCustomFormatter)
│   ├── ExtensionMethods.cs       CopyFrom/CopyPropertiesFrom, TransformTo, Log für Controller und Hub
│   ├── GlobalUsings.cs
│   ├── _requests.http
│   └── appsettings.json, Properties/launchSettings.json (http 5000, https 7001)
├── NorthwindDb/                  (mit DB) Klassenbibliothek mit EF Core + DbContext
└── MyBackendWpf/                 (Windows + addWPF) WPF-Client
```

Hilfsmethoden in `ExtensionMethods.cs`:

- `T CopyFrom<T>(this T target, object source)` / `CopyPropertiesFrom`: kopiert gleichnamige Properties, z.B. Entity → DTO
- `T TransformTo<T>(this object source)`: erzeugt z.B. einen `record` aus einem Objekt
- `this.Log("…")` in Controllern und SignalR-Hubs: schreibt Klasse und Methode auf die Konsole

## Gleich wie im Original vs. anders

**Gleich (Verhalten, nicht Code):**

| Bereich | |
|---|---|
| Aufruf | `createWebApi` / `createMinApi` / `createAllApi`, gleiche Parameter in gleicher Reihenfolge |
| Ports | http `5000`, https `7001`, zwei Profile in `launchSettings.json` |
| Projektstruktur | `.slnx`-Solution, Ordner `Dtos`, `Services`, `Controllers`, `Maps`, `Logging` |
| Features | Swagger, CORS ohne Einschränkungen, eigener Konsolen-Logger, Log-Level in `appsettings.json`, RestClientGenerator / `_requests.http` |
| Extension-Methoden | `CopyFrom`, `CopyPropertiesFrom`, `TransformTo`, `Log` (Controller und Hub), gleiche Signaturen |
| Datenbank | eigenes Projekt `<DbName>Db`, Scaffolding mit `dotnet ef`, `Northwnd` wird zu `Northwind`, `null` als Tabellenname = Code First mit `Dummies` |
| Test-Routen | `Values/Dummy`, `Values/<Tabelle>`, `dummy`, `dbTest/<Tabelle>` |
| NuGet-Pakete | gleiche Pakete und Versionen (Swashbuckle `10.2.3`, RestClientGenerator `1.3.1`, SignalRAnalyzer `1.7.4`, EF Core = dotnet-ef-Version) |
| Voraussetzung | .NET 10 SDK und dotnet-ef 10 |

**Anders:**

| Bereich | Original | xplat |
|---|---|---|
| Code | Windows-Batch (`.bat`) + `ReplaceInFile.exe` + `ExtractDbName.exe` | **komplett neu geschrieben**: eine C#-Datei (`create.cs`), läuft mit `dotnet run` |
| Betriebssysteme | nur Windows | **Windows, macOS, Linux** |
| Installation | Repo klonen, PATH selbst setzen | **Einzeiler-Installer**, der auch .NET 10 und dotnet-ef installiert |
| `.mdf` (SQL Server LocalDB) | ja | nur unter Windows (LocalDB gibt es nur dort). Auf macOS/Linux SQLite verwenden. |
| WPF-Client | WPF + npm + OpenAPI-Generator (braucht Node.js und Java) | nur unter Windows; einfacher Client mit `HttpClient`, ohne npm/Java. Auf macOS/Linux wird er mit Hinweis übersprungen. |
| Relative DB-Pfade | relativ zum Skript-Ordner | relativ zum **aktuellen** Ordner |
| Vorlagen | fünf `Program.cs`-Varianten | eine `Program.cs`-Vorlage, der Rest wird im Code ergänzt |
| Zielordner existiert schon | keine eigene Prüfung | bricht mit Meldung ab, nichts wird überschrieben |
| Fehler | Exit-Codes der Befehle werden nicht geprüft | bricht beim ersten fehlgeschlagenen Befehl mit klarer Meldung ab |
| macOS-Port 5000 | – | Warnung, wenn Port 5000 belegt ist (AirPlay) |

## Aufbau dieses Repos

| Datei | Zweck |
|---|---|
| `create.cs` | die gesamte Logik (.NET-10-*file-based app*) |
| `createWebApi`, `createMinApi`, `createAllApi` | Startskripte für macOS/Linux |
| `createWebApi.cmd`, … | Startskripte für Windows |
| `templates/` | Vorlagen der erzeugten Dateien. Platzhalter haben die Form `{{NAME}}`. |
| `install.sh`, `install.ps1` | Installer |

Der erste Aufruf dauert ein paar Sekunden länger, weil `dotnet` `create.cs` kompiliert. Danach kommt das Ergebnis aus dem Cache.

## Deinstallation

- **macOS/Linux:** `rm -rf ~/.createWebApiProject-xplat` und den Block zwischen `# >>> createWebApiProject-xplat >>>` und `# <<< createWebApiProject-xplat <<<` aus `~/.zshrc` bzw. `~/.bashrc` löschen.
- **Windows:** Ordner `%USERPROFILE%\.createWebApiProject-xplat` löschen und den Eintrag aus dem Benutzer-PATH entfernen (Einstellungen → „Umgebungsvariablen“).

Ein vom Installer installiertes .NET SDK liegt in `~/.dotnet` und kann bei Bedarf ebenfalls gelöscht werden.

## Getestet

- macOS (Apple Silicon), .NET SDK 10.0.402: alle drei Varianten, mit und ohne SQLite, Code First
- Linux (Docker `mcr.microsoft.com/dotnet/sdk:10.0`): dieselben Szenarien
- Installer `install.sh`: frisches Ubuntu 24.04 ohne .NET (installiert SDK, dotnet-ef, PATH; zweiter Lauf aktualisiert ohne doppelte Einträge) und macOS mit leerem HOME
- Windows: **noch nicht auf einem echten Windows-Rechner getestet.** Die WPF-Vorlage kompiliert, aber `.mdf`/LocalDB und `install.ps1` sind ungetestet. Feedback und Issues sind willkommen.

## Lizenz

[MIT](LICENSE). Gilt für den Code in diesem Repo. Das Original von Prof. Grüneis ist davon nicht betroffen.
