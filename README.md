<p align="center">
  <img src="branding/bingwalle.png" width="144" alt="BingWallE: Landschaftsbilder mit goldener Sonne" />
</p>

# BingWallE 1.0

[Release herunterladen](https://github.com/gruetze-software/BingWallE/releases/latest) ·
[Builds und Tests](https://github.com/gruetze-software/BingWallE/actions/workflows/build.yml)

BingWallE lädt Bing-Wallpaper herunter. Die Desktop-Anwendung verwendet C#,
.NET 8 und Avalonia 11.3.10. Das Repository liegt unter
[gruetze-software/BingWallE](https://github.com/gruetze-software/BingWallE).

## Funktionen

- Galerie mit zwei bis drei Spalten, großen Bildkarten und lesbaren Datumsangaben.
- Klick auf ein Motiv öffnet eine große Vorschau mit Bildbeschreibung und Copyright.
  Die Vorschau lässt sich mit „Schließen“ oder Escape schließen.
- „Alle“, „Keine“ und „Nur neue“ auswählen; gespeicherte Bilder sind gekennzeichnet.
- Bing-Region aus zwölf Ländern wählen, darunter Deutschland, USA, Japan und Frankreich.
- HD, Full HD oder UHD auswählen. Alternative Auflösungen hängen von Bing ab;
  bei fehlenden Varianten zeigt die Anwendung einen Fehler statt still herunterzustufen.
- Downloads mit Auswahlzähler, Fortschrittsanzeige, Abbruch und Wiederholen nach Fehlern.
- Speicherort ändern und direkt im Dateimanager öffnen.
- Helles, dunkles oder automatisches Systemdesign; Einstellungen werden gespeichert.

Pro Region werden bis zu acht Tagesbilder abgerufen. Nach einem Regionswechsel
lädt „Aktualisieren“ die neue Auswahl. Beim Ändern der Auflösung oder des Zielordners
wird die Dateierkennung aktualisiert: „Bereits gespeichert“ bezieht sich auf die
aktuell gewählte Auflösung im aktuellen Ordner. HD und UHD erhalten eigene Dateinamen,
damit vorhandene Full-HD-Bilder nicht überschrieben werden.

Bei einem Downloadfehler stoppt der Stapel. Bereits gespeicherte Bilder bleiben
abgewählt; „Erneut versuchen“ lädt nur die verbleibende Auswahl. Das Programm
verwendet temporäre Dateien, damit ein Fehler keine vorhandenen Bilder beschädigt.

BingWallE ist ein Sammler. Automatisches Herunterladen und das Setzen des
Desktop-Hintergrunds sind nicht implementiert.

## Voraussetzungen und Start

Empfohlen ist das **.NET-8-SDK**. Die `global.json` im Repository-Stamm erlaubt
auch ein neueres stabiles SDK, falls kein passendes 8er-SDK installiert ist.
Die Anwendung und Tests zielen weiterhin auf .NET 8; für `dotnet run` und
`dotnet test` muss die .NET-8-Laufzeit installiert sein.

Alle folgenden Befehle im Repository-Stamm ausführen:

```sh
dotnet restore BingWallE.sln
dotnet build BingWallE.sln -c Release
dotnet run --project BingWallE -c Release
```

Windows x64, Linux x64 und macOS (Intel und Apple Silicon) werden in der Build-Pipeline gebaut.
Die grafische Anwendung wurde nicht auf jeder Zielhardware manuell geprüft.
Linux benötigt eine grafische Desktop-Sitzung und die nativen Avalonia/.NET-Abhängigkeiten.

## Tests

```sh
dotnet test BingWallE.sln -c Release
```

Die xUnit-Tests prüfen Auswahl, Auflösungsvarianten, Vorschauabbruch, Wiederholen,
Abbruch, Fehlerbehandlung, Dateinamen, Bing-Antworten,
Einstellungen und Ordnerwechsel. Netzwerkantworten werden simuliert, Dateien in
isolierten temporären Ordnern angelegt. Es werden keine persönlichen Einstellungen
oder Bilder verändert. Die Tests ersetzen keinen manuellen Oberflächentest.

## Aufbau

| Bereich | Verantwortung |
| --- | --- |
| `Models` | Einstellungen, Bilddaten und plattformübergreifende Dateinamen |
| `Services/BingWallpaperClient` | Bing-Archiv und Vorschaudaten abrufen |
| `Services/WallpaperStore` | Vorhandene Bilder erkennen und Downloads sicher speichern |
| `Services/JsonSettingsStore` | Einstellungen laden und über eine temporäre Datei speichern |
| `Services/AvaloniaAppDialogs` | Ordnerauswahl und Informationsdialog |
| `ViewModels` | Bedienabläufe, Auswahl, Statusmeldungen und Vorschaubilder |
| `Views` | Avalonia-Oberfläche |
| `App.axaml.cs` | Dienste erzeugen und mit dem ViewModel verbinden |
| `BingWallE.Tests` | Automatisierte Tests mit austauschbaren Diensten |

Die Dienste werden über Schnittstellen an das ViewModel übergeben. Netzwerk- und
Dateizugriffe lassen sich dadurch unabhängig von der grafischen Oberfläche testen.

Einstellungen liegen unter `Environment.SpecialFolder.ApplicationData` im
Unterordner `bingwalle/settings.json`, unter Windows normalerweise
`%APPDATA%/bingwalle/settings.json`. Standardziel ist `BingImages` im Bilderordner.

## Pakete erstellen

Die Skripte ermitteln die Version aus `BingWallE/BingWallE.csproj` und schreiben
nach `artifacts/`. Die App zeigt die Haupt- und Nebenversion an: Paketversion
`1.0.0` entspricht der sichtbaren Version **1.0**. Jedes Paket enthält die
.NET-Laufzeit; Benutzer benötigen kein SDK.

| System | Befehl auf dem jeweiligen System | Ausgabe |
| --- | --- | --- |
| Windows | `pwsh -File scripts/build-windows.ps1` | ZIP mit `bingwalle.exe` |
| Linux | `bash scripts/build-linux.sh` | TAR.GZ und DEB |
| macOS Apple Silicon | `bash scripts/build-macos.sh arm64` | APP im ZIP und DMG |
| macOS Intel | `bash scripts/build-macos.sh x64` | APP im ZIP und DMG |

Windows und Linux unterstützen zusätzlich ARM64 über `-Architecture arm64`
beziehungsweise das Argument `arm64`; diese Varianten werden im ersten Release
nicht veröffentlicht. Unter Linux werden `dpkg-deb` und die üblichen nativen
Desktopbibliotheken benötigt. Das DEB installiert auch den Desktop-Eintrag und
App-Icon. Der ältere Einstieg `BingWallE/build.sh` ruft das passende Plattformskript auf.

Unter macOS die App aus dem DMG nach „Programme“ ziehen. Die Pakete sind ad-hoc
signiert, aber nicht Apple-notarisiert. Beim ersten Start kann eine Freigabe unter
„Datenschutz & Sicherheit“ nötig sein. Windows-Pakete sind nicht mit einem
Code-Signing-Zertifikat signiert. Eine globale Deaktivierung der Systemsicherheit
ist nicht erforderlich.

## Automatisierte Builds und Releases

`.github/workflows/build.yml` baut und testet auf Windows, Linux und macOS.
Die Pipeline erstellt Windows-ZIP, Linux-TAR.GZ/DEB sowie macOS-ZIP/DMG für Intel
und Apple Silicon. Testberichte und Pakete stehen als Workflow-Artefakte bereit.

Für Version 1.0 wird der Tag `v1.0` verwendet. Ein Tag-Build veröffentlicht das
GitHub-Release erst, wenn alle vier Build-Varianten erfolgreich sind. Alle Pakete
und eine Datei `SHA256SUMS.txt` werden angehängt. Release-Hinweise stehen unter
[docs/RELEASE-1.0.md](docs/RELEASE-1.0.md).

## Logos und Info-Dialog

Der Info-Dialog ist über **Info** oben rechts erreichbar und zeigt Version,
Projektlinks und das Grütze-Software-Logo aus AmiDiskLab.

<img src="BingWallE/Assets/gruetze-software.png" width="88" alt="Grütze-Software" />

Das neue BingWallE-Logo stellt gestapelte Landschaftsbilder und eine Sonne dar.
Die editierbare Vektorquelle liegt in [branding/bingwalle.svg](branding/bingwalle.svg).
Mit `python scripts/generate-icons.py` werden PNG, Windows-ICO und macOS-ICNS
neu erzeugt; dafür sind `Pillow` und `resvg-py` erforderlich. Die fertigen Icons
sind eingecheckt, daher benötigen normale Builds keine Python-Werkzeuge.

## Lizenz

Der Quellcode steht unter der MIT-Lizenz, siehe [LICENSE](LICENSE).

## Diagnoseausgabe

Beim Start mit F5 in Visual Studio: **Ansicht → Ausgabe**, dann in
**Ausgabe anzeigen von** den Eintrag **Debuggen** auswählen.

Die Traces enthalten Zeitstempel, eine Kennung pro HTTP-Anfrage, URL, HTTP-Status,
Dauer und Bytezahl. `START` ohne `HEADERS` bedeutet, dass noch kein Antwortkopf
angekommen ist. `HEADERS` ohne `COMPLETE` bedeutet, dass der Antwortinhalt noch
übertragen wird. Fehler werden einschließlich Stacktrace protokolliert.
Bildinhalte und HTTP-Header werden nicht ausgegeben.

Jede HTTP-Anfrage hat ein Zeitlimit von 30 Sekunden. Benutzerabbruch und
Zeitüberschreitung werden in der Oberfläche unterschieden. Während des Scans
zeigt die Statuszeile den aktuellen Tag und den Schritt (Bilddaten/Vorschau).
