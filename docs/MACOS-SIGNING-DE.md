# macOS-Releases automatisch signieren und notarisieren

Nach einer einmaligen Einrichtung erledigt GitHub Actions den gesamten Ablauf
für Apple Silicon und Intel: Developer-ID-Signatur, Apple-Notarisierung,
Anheften der Bestätigung an App und DMG sowie Veröffentlichung von ZIP und DMG.
Die ZIP wird erst nach dem Anheften an die App erzeugt. Es sind keine manuellen
Signierbefehle für jedes Release nötig. Zertifikat oder Schlüssel müssen später
erneuert werden, wenn sie ablaufen beziehungsweise widerrufen werden.

## 1. Das richtige Zertifikat auf dem Mac vorbereiten

Du brauchst eine aktive Apple-Developer-Program-Mitgliedschaft und ein
**Developer ID Application**-Zertifikat für Downloads außerhalb des Mac App Store.
Ein Apple-Development- oder App-Store-Vertriebszertifikat erfüllt diesen Zweck
nicht. Unsere ZIPs und DMGs benötigen kein Developer-ID-Installer-Zertifikat.

Erstelle beziehungsweise installiere das Zertifikat gemäß
[Apples Developer-ID-Anleitung](https://developer.apple.com/developer-id/).
Exportiere es in der Schlüsselbundverwaltung deines Macs **zusammen mit dem
zugehörigen privaten Schlüssel** als passwortgeschützte `.p12`-Datei. Der
Schlüssel muss dort dem Zertifikat zugeordnet sein; eine `.cer`-Datei allein
reicht für den GitHub-Build nicht aus.

Um die P12 für GitHub vorzubereiten, im Mac-Terminal ausführen; den Pfad anpassen:

```bash
base64 -i "$HOME/Downloads/BingWallE.p12" | tr -d '\n' | pbcopy
```

Der Inhalt liegt anschließend in der Zwischenablage für das erste Secret.

## 2. Zugang für die Apple-Notarisierung vorbereiten

Erstelle in **App Store Connect → Users and Access → Integrations → App Store
Connect API** einen **Team-API-Schlüssel** mit den benötigten Berechtigungen für
die Notarisierung. Notiere **Key ID** und **Issuer ID**, und lade die `.p8`-Datei
herunter. Verwende hier einen Team-Schlüssel, da unser Ablauf mit einer Issuer ID
arbeitet. Zertifikat und API-Schlüssel müssen zum passenden Apple-Team gehören.

Der Schlüssel dient nur zur Anmeldung bei Apples Notarisierungsdienst; dieser
Ablauf veröffentlicht die App nicht im Mac App Store. Dein normales Apple-Passwort
wird nicht benötigt.

## 3. Fünf GitHub-Secrets einmalig hinterlegen

Im BingWallE-Repository **Settings → Secrets and variables → Actions → Secrets →
New repository secret** öffnen:

[GitHub-Secrets für BingWallE](https://github.com/gruetze-software/BingWallE/settings/secrets/actions)

| Name | Inhalt |
| --- | --- |
| `MACOS_CERTIFICATE_P12_BASE64` | Der Base64-Inhalt der P12 aus Schritt 1 |
| `MACOS_CERTIFICATE_PASSWORD` | Das beim P12-Export vergebene Passwort |
| `APPLE_API_KEY_P8` | Vollständiger Text der P8-Datei einschließlich BEGIN/END-Zeilen und Zeilenumbrüchen |
| `APPLE_API_KEY_ID` | Key ID des Team-API-Schlüssels |
| `APPLE_API_ISSUER_ID` | Issuer ID des Apple-Teams |

Diese Inhalte gehören ausschließlich in die Secrets, nicht in Git-Dateien oder
Chatnachrichten. Die Zertifikatsidentität wird automatisch aus der P12 ermittelt.
Der Build erzeugt sein eigenes temporäres Schlüsselbund-Passwort und räumt
Schlüsselbund und temporäre Schlüsseldateien beim Beenden auf.

## 4. Einen signierten Test-Build starten

Nachdem diese Änderungen auf `main` liegen, unter **Actions → Build, test and
release → Run workflow** den Branch `main` auswählen:

- **release_tag** leer lassen.
- **sign_macos** aktivieren.

Alternativ mit der GitHub CLI:

```bash
gh workflow run build.yml --repo gruetze-software/BingWallE --ref main -f sign_macos=true
```

Dieser Lauf signiert und notarisiert die macOS-Pakete, veröffentlicht aber kein
GitHub-Release. Nach erfolgreichem Lauf die macOS-Artefakte herunterladen und
auf deinem Mac insbesondere ersten Start, Vorschau und lokale Bilderkennung
prüfen. Ein echter Test der Apple-Freigabe ist erst mit deinen Secrets möglich.

## 5. Automatik für alle folgenden Releases aktivieren

Unter **Settings → Secrets and variables → Actions → Variables** eine
Repository-Variable anlegen:

```text
Name:  MACOS_SIGNING_ENABLED
Value: true
```

[GitHub-Variablen für BingWallE](https://github.com/gruetze-software/BingWallE/settings/variables/actions)

Ab dann genügt der bisherige Release-Ablauf mit einem neuen Versions-Tag. Der
Workflow signiert und notarisiert die macOS-Downloads automatisch. Dieselbe
Automatik gilt für einen manuellen Veröffentlichungslauf mit `release_tag`.
Der betreffende Tag muss die neuen Signierskripte enthalten. Bereits
veröffentlichte Pakete wie 1.6.2 werden nicht nachträglich verändert.

Normale Push-/PR-Tests verwenden keine Apple-Secrets und bleiben ad-hoc signiert.
Wenn die Signierautomatik aktiviert ist und Zugangsdaten fehlen oder Apple die
Notarisierung ablehnt, schlägt der Release-Lauf fehl; es gibt keinen automatischen
Rückfall auf ad-hoc signierte Release-Pakete.

## Technischer Ablauf

`scripts/build-macos-signed.sh` importiert das Zertifikat in einen temporären
Schlüsselbund. `scripts/build-macos.sh` signiert zunächst die eingebetteten
Mach-O-Dateien und dann die App mit Zeitstempel und Hardened Runtime. Die App
erhält die für .NET/Avalonia erforderliche `allow-jit`-Berechtigung, aber keine
Debugging-Berechtigung und keine pauschale Ausnahme von der Library Validation.

`scripts/notarize-macos.sh` lädt einmal hoch und wartet bei Verzögerungen erneut
auf dieselbe Submission. Nur ein Status `Accepted` erlaubt das Anheften und
Validieren der Bestätigung. Danach werden die ZIP mit der bestätigten App und
die signierte, separat notarisierte DMG erzeugt und mit Gatekeeper geprüft.
Apple-Verzögerungen können den Build verlängern; macOS-Jobs haben deshalb
90 Minuten Zeitbudget. Im Fehlerfall wird die Submission-ID protokolliert.

## Offizielle Quellen

- [Apple: Developer ID](https://developer.apple.com/developer-id/)
- [Apple: Notarisierung automatisieren](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow)
- [GitHub: Apple-Zertifikate auf macOS-Runnern installieren](https://docs.github.com/en/actions/how-tos/deploy/deploy-to-third-party-platforms/sign-xcode-applications)
- [Microsoft: .NET-Apps für macOS veröffentlichen](https://learn.microsoft.com/en-us/dotnet/core/deploying/macos)
- [Avalonia: macOS-Verteilung](https://docs.avaloniaui.net/docs/deployment/macos)
