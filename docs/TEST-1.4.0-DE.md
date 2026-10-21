# BingWallE 1.4.0 – Testleitfaden

Testschritte und Funktionsübersicht für Version 1.4.0. Die Neuerungen stehen in [RELEASE-1.4.0.md](RELEASE-1.4.0.md). Store-Aktualisierungen erfolgen unabhängig vom GitHub-Release.

## Neu

- Einstellungen: „Windows-Blickpunkt zusätzlich laden“, standardmäßig ausgeschaltet.
- Bing bleibt aktiv. Blickpunkt-Bilder erscheinen in derselben Galerie und Vorschau.
- Die vorhandene Sprache/Region der Downloads gilt für beide Bildquellen, unabhängig von der App-Sprache.
- Quellenanzeige an jedem Bild; Herkunft, stabile Bildkennung, Inhaltsregion, Bildadresse und vorhandene Bildnachweise werden gespeichert (XMP sowie Windows-Kommentar/Schlagwörter).
- Blickpunkt wird in der gelieferten Originalauflösung gespeichert. HD/Full HD/UHD ändern weiterhin nur Bing-Bilder.
- Ein fehlendes Erstellungs- oder Aufnahmedatum wird mit dem Downloadzeitpunkt ergänzt; vorhandene Daten bleiben erhalten. XMP enthält zusätzlich das Downloaddatum und kennzeichnet ersatzweise verwendete Downloadzeiten. Das Veröffentlichungsdatum bleibt davon getrennt.
- Neue Dateinamen enthalten nur ASCII-Buchstaben, Ziffern und Unterstriche. Akzente werden vereinfacht, Satzzeichen entfernt/ersetzt. Frühere Dateinamen werden weiterhin erkannt und nicht automatisch umbenannt. Titel und Beschreibungen in den Metadaten behalten ihre Originalschreibweise.
- Stabile Bildkennungen unterscheiden Blickpunkt-Motive auch bei gleichen bereinigten Titeln.
- Ein Ausfall einer Quelle lässt bereits geladene Bilder der anderen Quelle verfügbar und wird angezeigt.

- Textbuttons sind zentral horizontal und vertikal ausgerichtet. Nur der Ko-fi-Unterstützungsbutton zeigt ein Kaffee-Symbol. Aktualisieren wechselt während einer laufenden Aktion zu Abbrechen.

## Bitte testen

1. Anwendung starten; im Info-Dialog muss 1.4.0 stehen.
2. In Einstellungen Blickpunkt aktivieren, Bildregion Deutschland wählen, schließen und aktualisieren.
3. Bing- und Blickpunkt-Karten prüfen; Vorschau und Bildnachweis öffnen.
4. Einige Bilder speichern; im Windows-Explorer unter Eigenschaften/Details Kommentare und Schlagwörter prüfen.
5. Auf USA wechseln und erneut aktualisieren: Inhalte beider Quellen sollten Englisch sein; die App-Sprache bleibt unverändert.
6. Nochmals laden und verschiedene Bing-Auflösungen wählen: Blickpunkt-Dateien bleiben in Originalauflösung und werden als vorhanden erkannt.
7. Anwendung neu starten: Einstellung bleibt erhalten. Blickpunkt ausschalten und aktualisieren: nur Bing wird geladen.

## Technischer Hinweis

Die Blickpunkt-Schnittstelle ist keine offiziell unterstützte öffentliche Entwickler-API und kann sich ändern. Angefordert werden nur die Auswahl und die Bilder, keine in Antworten enthaltenen Tracking-/Impression-Links.
Protokollreferenz: https://github.com/ORelio/Spotlight-Downloader/blob/master/SpotlightAPI.md
Der Client wurde eigenständig implementiert; es wurde kein Quellcode dieses Projekts übernommen.
