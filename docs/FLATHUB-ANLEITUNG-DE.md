# BingWallE auf Flathub: deine Schritt-für-Schritt-Anleitung

Stand: 18. Oktober 2026. Für deinen Ubuntu-Rechner, Linux Mint oder eine Linux-VM
mit grafischer Oberfläche. Das Ziel ist ein getestetes Flatpak, anschließend die
Einreichung bei Flathub und die Verifizierung als offizieller Herausgeber.

Diese Anleitung ist KI-unterstützte Recherche und Projekt-Dokumentation. Sie
enthält weder ein Manifest noch Texte zum Einfügen in einen Flathub-Pull-Request.

## 1. Was bereits vorbereitet ist

| Punkt | Stand |
| --- | --- |
| Anwendung | BingWallE, Entwicklungsversion 1.3.3 |
| Quellcode | https://github.com/gruetze-software/BingWallE |
| Vorbereitete Änderungen | https://github.com/gruetze-software/BingWallE/pull/1 |
| Kennung der Linux-Anwendung | `de.gruetze_software.BingWallE` |
| Deine Domain | `gruetze-software.de` |
| Technische Basis | .NET 10 und Avalonia |
| Programmdatei | `bingwalle` |
| Metadaten | `branding/de.gruetze_software.BingWallE.metainfo.xml` |
| Linux-Menüeintrag | `branding/de.gruetze_software.BingWallE.desktop` |
| App-Symbol | `BingWallE/Assets/bingwalle.png` |
| Linux-App-Ansichten | `branding/screenshots/1.3.3/` |
| Unterstützung | https://ko-fi.com/gruetzesoftware |

Die Plattform-Builds wurden geprüft. Ein erfolgreicher DEB- oder Snap-Build ist
aber noch kein Flatpak-Test. Das Manifest und die Sandbox-Prüfung fehlen bisher.
Die App-Ansichten stammen aus der tatsächlichen Linux-Renderausgabe mit echten
Bing-Bilddaten; sie zeigen keine Mint-Fensterdekorationen und keine Flatpak-Sitzung.

## 2. Die Grenze bei der KI-Unterstützung

Flathub untersagt KI-generierte oder KI-unterstützte Manifestinhalte. Auch
Einreichungstexte, zugehörige Commit-Nachrichten und Review-Antworten müssen
menschlich verfasst werden. Schreibe das Manifest deshalb eigenständig anhand
der offiziellen Dokumentation oder bitte einen menschlichen Paketbetreuer darum.
Bloßes Abtippen eines KI-Manifests würde diese Vorgabe nicht erfüllen.

KI-Anteile im übrigen Projekt musst du mit betroffenen Bereichen und ungefährer
Größenordnung offenlegen. Bei BingWallE betrifft das unter anderem Code,
Dokumentation und Grafik. Prüfe den Umfang selbst; eine Annahme ist nicht garantiert.

Quelle: [Flathub-Anforderungen und KI-Regeln](https://docs.flathub.org/docs/for-app-authors/requirements#generative-ai-policy).

## 3. Werkzeuge auf deinem Linux-Rechner installieren

Öffne ein Terminal. Auf Ubuntu beziehungsweise Mint:

```bash
sudo apt update
sudo apt install git flatpak
flatpak remote-add --if-not-exists --user flathub https://dl.flathub.org/repo/flathub.flatpakrepo
flatpak install --user flathub org.flatpak.Builder
```

Mint bringt Flatpak häufig bereits mit. Wir verwenden hier bewusst eine
Benutzerinstallation. Für Builds im Builder brauchst du keinen separat auf dem
Host installierten .NET-SDK. Die Laufzeit- und SDK-Auswahl gehört zur Paketierung.

Prüfen:

```bash
flatpak --version
flatpak info --user org.flatpak.Builder
```

Erwartet: Beide Befehle liefern Versions- beziehungsweise Paketinformationen.
Wenn nach einer Neuinstallation die Desktop-Integration fehlt, einmal ab- und
wieder anmelden. Quellen: [Flatpak-Grundlagen](https://docs.flatpak.org/en/latest/using-flatpak.html)
und [Builder-Werkzeug](https://github.com/flathub/org.flatpak.Builder).

## 4. Einen getrennten Arbeitsordner anlegen

Diese Befehle überschreiben keinen bestehenden BingWallE-Checkout:

```bash
mkdir -p ~/BingWallE-Flathub-Arbeit
cd ~/BingWallE-Flathub-Arbeit
git clone --branch codex/flathub-preparation --single-branch https://github.com/gruetze-software/BingWallE.git BingWallE
mkdir -p paket
```

Falls der Zielordner bereits existiert, nicht löschen: Verwende einen anderen
Arbeitsordner. Der Branch enthält die vorbereiteten Änderungen, ist aber noch
keine veröffentlichte Version. Nach dem Zusammenführen beziehungsweise einer
Veröffentlichung muss die Quelle entsprechend aktualisiert werden.

## 5. Das Paketmanifest selbst erstellen

Arbeite im Ordner `~/BingWallE-Flathub-Arbeit/paket`.

Lies zuerst die [offizielle .NET-Paketierungsanleitung](https://docs.flatpak.org/en/latest/dotnet.html)
und die dort verlinkten Manifest-Grundlagen. Das dortige Beispiel verwendet noch
.NET 8 und eine ältere Runtime: Nicht unverändert übernehmen. Wähle eine aktuell
gepflegte Runtime und die passende .NET-10-Erweiterung; deren Angaben findest du
im [offiziellen Erweiterungsprojekt](https://github.com/flathub/org.freedesktop.Sdk.Extension.dotnet10).

Erstelle deine eigene Manifestdatei. Für die folgenden Testbefehle nehmen wir
den Dateinamen `de.gruetze_software.BingWallE.yaml` an. Wenn du einen anderen
zulässigen Dateityp verwendest, passe die Befehle entsprechend an.

Die .NET-Anleitung erklärt außerdem die Vorbereitung der NuGet-Quellen mit dem
offiziellen Generator. Verwende dessen aktuelle Hilfe und die tatsächliche
Projektdatei `BingWallE/BingWallE.csproj`. Das spätere Bauen muss ohne spontane
NuGet-Downloads funktionieren. Prüfe die erzeugten Quellen selbst.

Zur Orientierung im Quellprojekt: Menüeintrag, Symbol und Metadaten müssen auch
im installierten Flatpak zusammenpassen. Eine vorhandene DEB-Datei allein ist
kein Ersatz für die Flatpak-Paketierung.

**Hier ist menschliche Paketierungsarbeit erforderlich.** Erst weitergehen, wenn
dein Manifest und seine benötigten Quelldateien tatsächlich vorhanden sind.

## 6. Das erste Flatpak bauen und starten

Im Paketordner mit deinem selbst geschriebenen Manifest:

```bash
cd ~/BingWallE-Flathub-Arbeit/paket
flatpak run --command=flathub-build org.flatpak.Builder --install de.gruetze_software.BingWallE.yaml
flatpak run de.gruetze_software.BingWallE
```

Erwartet: Der Build endet erfolgreich und anschließend öffnet sich BingWallE.
Dieser erste Lauf installiert dein lokales Testpaket, veröffentlicht aber nichts.
Bei Abweichungen den Build zuerst korrigieren, nicht direkt zur Einreichung springen.

Für eine Protokolldatei beim App-Start:

```bash
flatpak run de.gruetze_software.BingWallE 2>&1 | tee bingwalle-flatpak-start.log
```

## 7. Die Anwendung wirklich in der Sandbox testen

Starte für diese Prüfung ausdrücklich mit `flatpak run`, damit du nicht
versehentlich die vorhandene Snap- oder DEB-Version testest.

- Deutschland und USA aktualisieren; Vorschau, Copyright, Auflösung und Tags ansehen.
- Einen Bilderordner über die Ordnerauswahl wählen und Bilder herunterladen.
- Danach neu starten und prüfen, ob Ordnerzugriff und Einstellungen erhalten bleiben.
- „Show“ und „Open Path“ sowie den Ko-fi-Link im About-Dialog ausprobieren.
- Optional die lokale Bilderkennung aktivieren: Modell laden, Tags prüfen, beenden
  und wieder öffnen. Den Modell-Download nicht als Kamera- oder Bild-Upload deuten.
- Deutsch/Englisch, Hell/Dunkel, kleine Fenstergröße und Maximierung testen.
- Ignorieren prüfen; Löschen nur mit einer eigens dafür angelegten Testdatei testen.

Die tatsächlich gewährten Berechtigungen kannst du ansehen:

```bash
flatpak info --show-permissions de.gruetze_software.BingWallE
```

Wenn Dateizugriff oder externe Links scheitern, muss die Portal- beziehungsweise
Desktop-Integration untersucht werden. Nicht einfach pauschal den gesamten
Rechner freigeben. Im aktuellen Quellcode gibt es unter Linux auch `xdg-open`-
Aufrufe; ihr Verhalten in der Sandbox ist bisher nicht bestätigt.

## 8. Die Paketprüfungen ausführen

Im Paketordner:

```bash
flatpak run --command=flatpak-builder-lint org.flatpak.Builder manifest de.gruetze_software.BingWallE.yaml
```

Wenn dein Build das lokale OSTree-Repository `repo` erzeugt hat, zusätzlich:

```bash
flatpak run --command=flatpak-builder-lint org.flatpak.Builder repo repo
```

Die Manifestprüfung allein reicht nicht. Falls `repo` nicht existiert, prüfe
anhand der Build-Dokumentation, wo das Repository erstellt wird. Fehler vor der
Einreichung beheben. Für Ausnahme-Anträge gelten eigene Regeln; KI darf auch diese
nicht verfassen oder abwickeln.

Quelle: [Flathub-Paketprüfungen](https://docs.flathub.org/docs/for-app-authors/linter).

## 9. Vor der Einreichung: veröffentlichte Quelle prüfen

Für die Stable-Einreichung ist der Upstream-Release `v1.3.3` vorgesehen. Prüfe,
dass der [GitHub-Release](https://github.com/gruetze-software/BingWallE/releases/tag/v1.3.3)
mit fertigen Paketen veröffentlicht ist. Der zugehörige AppStream-Eintrag ist
als stabile Version markiert. Verwende den exakten Commit dieses Tags und prüfe
das damit erneut gebaute Flatpak. Release-Veröffentlichung im eigenen Projekt
und Flathub-Einreichung bleiben zwei getrennte Vorgänge.

Prüfe außerdem die Bildrechte und die Eignung der Screenshots. Die bereits
vorhandenen README-Screenshots dürfen erhalten bleiben; sie sind keine Garantie,
dass sämtliche Flathub-Vorgaben erfüllt sind.

## 10. Die Einreichung selbst durchführen

Folge der [offiziellen Einreichungsseite](https://docs.flathub.org/docs/for-app-authors/submission#submission-pr):

1. Auf GitHub einen Fork von `flathub/flathub` anlegen, einschließlich des Branches
   `new-pr`. Nicht nur den Hauptbranch kopieren.
2. Den Fork klonen und einen eigenen Arbeitsbranch auf Basis von `new-pr` anlegen.
3. Deine Paketdateien hinzufügen; Manifest und Quelldefinitionen gehören hierhin,
   keine Kopie des kompletten App-Quellcodes oder der fertigen Programmdatei.
4. Commit-Nachricht selbst schreiben, Änderungen hochladen und den Pull-Request
   über die GitHub-Weboberfläche gegen `flathub/flathub`, Zielbranch `new-pr`, öffnen.
5. Die angezeigte Vorlage selbst ausfüllen und die bekannten KI-Anteile ehrlich
   offenlegen. Titel, Beschreibung und Antworten eigenständig formulieren.

Für diese Nachrichten gibt es hier bewusst keinen vorbereiteten Text. Bei
Rückfragen denselben Pull-Request weiterbearbeiten. Reviewer-Anweisungen zu
Test-Builds beachten; nicht automatisch neue Einreichungen anlegen.

## 11. Nach der Annahme: Grütze-Software verifizieren

Nimm die Einladung zum Flathub-App-Repository zeitnah an. Dafür muss GitHub-2FA
aktiviert sein. Melde dich anschließend auf Flathub an und öffne im
**Developer Portal → deine App → Verification** die angebotene Prüfung.

Bei unserer Domain-Kennung ist die Website-Methode möglich. Den vom Portal
gelieferten Token hinterlegst du als Textdatei unter:

```text
https://gruetze-software.de/.well-known/org.flathub.VerifiedApps.txt
```

Verwende ausschließlich den echten Token aus dem Portal. Vorhandene Einträge für
andere Apps behalten; jeder Token bekommt eine eigene Zeile. Die Adresse muss
öffentlich über HTTPS erreichbar sein. Das Portal kann alternativ auch eine
DNS-Prüfung anbieten. Die Tokenprüfung erst ausführen, wenn die Datei erreichbar ist.

Quelle: [offizielle Verifizierung](https://docs.flathub.org/docs/for-app-authors/verification).

## 12. Abschließende Kontrolle unter Mint

Nach Veröffentlichung und Verifizierung in der Mint-Anwendungsverwaltung nach
**BingWallE** suchen. Prüfe Herausgeber, Screenshots, Beschreibung und Installation.
Veröffentlichung, Suchindex und lokaler Cache können zeitlich auseinanderliegen.
Das Snap-Banner ist nicht automatisch ein Flathub-Banner.

## Dein sinnvoller nächster Schritt

**Zuerst Abschnitt 3 und 4 auf deinem Ubuntu- oder Mint-Rechner durchführen.**
Danach kommt das selbst geschriebene Manifest aus Abschnitt 5. Die Prüfliste
[FLATHUB.md](FLATHUB.md) enthält ergänzend den technischen Vorbereitungsstand.
