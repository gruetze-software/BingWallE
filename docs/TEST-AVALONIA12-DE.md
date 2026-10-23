# Testpakete fuer Linux und macOS

Aktueller Quellstand: `main`, Version 1.5.0 mit .NET 10 und Avalonia 12.1.4.
Die Wayland-Umstellung und die korrigierte Bildvorschau sind uebernommen.
Fuer weitere Tests die Artefakte des aktuellen main-Builds verwenden.
Aeltere Pakete des Testbranches `codex/avalonia12-wayland` tragen noch 1.4.1;
sie sind nicht identisch mit dem regulaeren 1.4.1-Release. Ein Merge allein
veroeffentlicht keine neuen Release- oder Store-Pakete.
Es wird keine Entwicklungsumgebung und kein separates .NET benoetigt.

## 1. Die richtigen Pakete herunterladen

Oeffne den bereitgestellten GitHub-Actions-Lauf fuer `main`. Melde dich
bei GitHub an und scrolle auf der Zusammenfassung zu **Artifacts**.

| System | Artefakt | Inhalt zum Testen |
| --- | --- | --- |
| Linux x64 | `packages-linux-x64` | TAR.GZ, alternativ DEB |
| Mac mit Apple Silicon | `packages-osx-arm64` | ZIP mit BingWallE.app, alternativ DMG |
| Mac mit Intel-Prozessor | `packages-osx-x64` | ZIP mit BingWallE.app, alternativ DMG |

Der Artefakt-Download selbst ist ebenfalls ein ZIP. Diesen zuerst entpacken,
danach das darin enthaltene Anwendungspaket entpacken. Pruefberichte unter
`tests-*` enthalten keine startbare Anwendung. Fuer einen Mac wird macOS 14
oder neuer benoetigt. Prozessor und macOS-Version stehen unter
**Apple-Menue > Ueber diesen Mac**.

## 2. Linux in deiner Ubuntu- oder Mint-VM

Fuer den ersten Test das TAR.GZ in einen eigenen neuen Ordner entpacken. Damit
musst du keine vorhandene Snap- oder DEB-Installation ersetzen. Die entpackte
Anwendung verwendet normalerweise dieselben Einstellungen wie die regulaere
Archiv-/DEB-Version. Oeffne ein Terminal im entpackten Ordner mit der Datei
`bingwalle`. Fuer ein getrenntes Testprofil kannst du dort vor dem Start setzen:

```bash
export XDG_CONFIG_HOME="$PWD/test-profile/config"
export XDG_DATA_HOME="$PWD/test-profile/data"
mkdir -p "$XDG_CONFIG_HOME" "$XDG_DATA_HOME"
```

Starte anschliessend im selben Terminal:

```bash
chmod +x bingwalle
./bingwalle 2>&1 | tee test-linux-auto.log
```

Waehle in der Anwendung einen eigenen Bilderordner fuer Testdownloads.
Starte bewusst mit `./bingwalle`, damit du nicht die installierte Snap-Version
mit dem gleichnamigen Befehl startest. Eine grafische Desktop-Sitzung und die
nativen Linux-Bibliotheken sind weiterhin erforderlich. Falls ein Bibliotheksfehler
auftritt, notiere die genaue Meldung; die DEB-Variante deklariert ihre Abhaengigkeiten
und kann alternativ ueber APT installiert werden.

### X11 und natives Wayland unterscheiden

Pruefe im Terminal:

```bash
echo "$XDG_SESSION_TYPE"
```

Bei `x11` pruefst du die klassische Linux-Oberflaeche. Fuer natives Wayland
brauchst du eine Wayland-Sitzung mit geeignetem Compositor, etwa unter einem
aktuellen Ubuntu-/GNOME-Desktop. Mint in einer X11-Sitzung eignet sich fuer den
X11-Test, ist aber damit noch kein nativer Wayland-Test.

Schliesse die App zwischen den folgenden Starts:

```bash
BINGWALLE_LINUX_BACKEND=wayland ./bingwalle 2>&1 | tee test-linux-wayland.log
BINGWALLE_LINUX_BACKEND=x11 ./bingwalle 2>&1 | tee test-linux-x11.log
```

Der erste Befehl erzwingt Wayland und faellt absichtlich nicht auf X11 zurueck.
Er funktioniert nur bei erreichbarem, ausreichend aktuellem Wayland-Compositor.
Im Protokoll muss beim nativen Start stehen:

```text
BingWallE display backend: Avalonia.Wayland.WindowImpl
```

Beim X11-Start steht dort `Avalonia.X11.X11Window`. Ein sichtbares Fenster in
einer Wayland-Sitzung allein beweist kein natives Wayland: Die automatische
Auswahl kann XWayland verwenden. Bei einer Startstoerung pruefe die automatische
Variante ohne Backend-Variable und bewahre das Protokoll auf.

Das TAR.GZ testet die Linux-App ohne Flatpak-Sandbox. Ein echter Flatpak-Test
mit Portal-Ordnerwahl und Sandbox-Berechtigungen bleibt ein separater Schritt;
das bestehende Flathub-Manifest wurde nicht geaendert.

## 3. macOS auf einem Mac testen

Lade das ZIP fuer die passende Architektur herunter und entpacke `BingWallE.app`
in einen eigenen Testordner. Zum Testen ist keine Ablage in Programme erforderlich;
so ersetzt du deine vorhandene Anwendung nicht. Schliesse eine bereits laufende
BingWallE-Version und starte die App aus dem Testordner.

Die Test-App verwendet die normalen BingWallE-Einstellungen deines Mac-Benutzers.
Ein eigener macOS-Testbenutzer trennt Einstellungen und Bilder bei Bedarf vollstaendig.

Die Pakete sind ad-hoc signiert, aber nicht von Apple notarisiert. Falls macOS den
Start blockiert, versuche ihn einmal und gehe danach zu **Systemeinstellungen >
Datenschutz & Sicherheit > Dennoch oeffnen** fuer genau diese Test-App. Siehe
[Apples Anleitung](https://support.apple.com/en-au/102445).

Fuer eine Startprotokolldatei oeffne ein Terminal im Ordner, der die App enthaelt:

```bash
./BingWallE.app/Contents/MacOS/bingwalle 2>&1 | tee test-macos.log
```

Die macOS-Oberflaeche kann nicht auf Windows nativ geprueft werden. Dafuer brauchst
du Zugriff auf einen Mac; die GitHub-Builds ersetzen den interaktiven Test nicht.

## 4. Auf beiden Systemen pruefen

- Bing und optional Spotlight laden; deutsche und englische Bildregion pruefen.
- Bildvorschau: schmale Darstellung, kein Text unter der Scrollleiste, Status
  auch beim Scrollen sichtbar; keine Download-Checkbox bei gespeicherten Bildern.
- Neuen Zielordner auswaehlen, Bilder herunterladen, abbrechen und erneut starten.
- Bereits gespeicherte Bilder erkennen; Show, Open Path und Loeschen einer
  eigens angelegten Testdatei ausprobieren.
- Einstellungen und About: Deutsch/Englisch, Hell/Dunkel, Buttons und Kaffee-Link.
- Fenster verkleinern, maximieren und erneut starten; Bildschirmskalierung pruefen.
- Unter macOS zusaetzlich Menueleiste, Tastaturfokus, Dialoge und Finder pruefen;
  unter Linux Kontextmenues, Dialogfokus und Skalierung auf dem nativen Backend.
- Erwartete HTTP-Abbrueche nach Schliessen einer Vorschau sind keine Abstuerze.
  Bei Problemen Betriebssystem, Paketarchitektur, Backend-Zeile und das passende
  Startprotokoll festhalten.

## Quellen

- [GitHub-Artefakte herunterladen](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts)
- [Avalonia Linux / Wayland](https://docs.avaloniaui.net/docs/platform-specific-guides/linux)
- [Weitere technische Details](WAYLAND.md)
