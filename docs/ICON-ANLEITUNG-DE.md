# BingWallE-Icon in Paint.NET überarbeiten

Diese Anleitung verwendet die offiziellen
[Flathub-Empfehlungen für App-Icons](https://docs.flathub.org/docs/for-app-authors/metainfo-guidelines/quality-guidelines#app-icon).
Sie betreffen die Qualität der Store-Darstellung; sie sind keine Garantie für
eine Freigabe. Das bestehende, von Christoph gezeichnete Motiv bleibt die Grundlage.

## 1. Mit einer Arbeitskopie beginnen

Öffne `bingwalle_icon.pdn` in Paint.NET und speichere zunächst eine Arbeitskopie.
Behalte die Ebenen für Hintergrund, Sonne, Berge und Fluss, damit sie bearbeitbar
bleiben. Die Leinwand bleibt **1024 × 1024 Pixel**, außen transparent.

## 2. Das offizielle SVG-Raster als Hilfsebene verwenden

- [Offizielle SVG-Vorlage herunterladen](https://docs.flathub.org/img/app-icon-grid.svg)
- [Unveränderte Kopie im Projekt](../branding/reference/flathub-app-icon-grid.svg)

Die Vorlage enthält eine abgerundete quadratische und eine kreisförmige
Orientierung. Für unseren abgerundeten Hintergrund ist die quadratische Form
die passende Orientierung. Der äußere Rand der Datei ist die Leinwand, nicht
die gewünschte Außenkante unseres Motivs.

Falls Paint.NET die SVG nicht direkt öffnen kann, exportiere sie zunächst mit
einem SVG-fähigen Programm als **1024 × 1024 Pixel große transparente PNG**.
Eine kleine PNG erst nachträglich zu vergrößern macht die Hilfslinien unscharf.

Importiere diese PNG in Paint.NET über **Ebenen → Aus Datei importieren** und
lege sie ganz oben ab. Benenne die Ebene `Flathub-Raster – nicht exportieren`.
Das Raster muss die gesamte Leinwand abdecken und bei Position **0, 0** liegen.
Seine Linien sind schwach transparent: Zum Ausrichten kannst du darunter
vorübergehend eine weiße Hilfsebene einblenden.

## 3. Größe und Position des ganzen Motivs anpassen

Aus der Geometrie dieser SVG ergibt sich bei einer 1024er Leinwand für die
quadratische Orientierung ungefähr:

| Eigenschaft | Praktischer Ausgangswert |
| --- | --- |
| Sichtbare Grundform | 824 × 824 Pixel |
| Position oben links | 100, 100 |
| Transparenter Rand | Rund 100 Pixel je Seite |

Das sind **aus der Vorlage abgeleitete Richtwerte**, keine von Flathub
vorgeschriebenen Pixelgrenzen. Die kreisförmige Orientierung hat andere Maße.

Skaliere und verschiebe Sonne, Berge, Fluss und Grundform gemeinsam, damit ihre
Abstände zueinander erhalten bleiben. Für diesen Schritt kannst du in einer
zusätzlichen Arbeitskopie die Motiv-Ebenen zusammenführen. Die SVG-Hilfsebene
und Hilfshintergründe bleiben getrennt.

Wähle zum Skalieren das sichtbare Motiv aus. **Strg+A wählt die gesamte Leinwand
einschließlich transparenter Ränder**; deren Auswahlgröße entspricht daher nicht
automatisch der sichtbaren Icon-Größe. Verkleinere proportional und richte die
Grundform an der quadratischen Rasterlinie aus.

Für die genaue Zentrierung gilt pro Achse:

```text
Position = (Leinwandgröße − sichtbare Motivgröße) / 2
Beispiel: (1024 − 824) / 2 = 100
```

Gleiche X- und Y-Werte allein beweisen noch keine Zentrierung: Auch der rechte
und untere Abstand müssen passen. Eine 912 Pixel breite Form hätte beispielsweise
bei X = 56 gleiche Seitenränder; bei X = 58 wären es links 58 und rechts 54 Pixel.

## 4. Farben und Konturen prüfen

Flathub empfiehlt einen gut erkennbaren Fußabdruck, Kontrast auf hellen und
dunklen Flächen und ein auch klein lesbares Motiv. Eingebaute Schatten und
Leuchteffekte sollen entfallen; der Store ergänzt seinen eigenen Schatten.

Für BingWallE bedeutet das:

- Sonne, Berge und Fluss als klare Flächen erhalten; schmale Ausläufer vereinfachen.
- Kanten sauber glätten. Starke Weichzeichnung macht das kleine Icon verwaschen.
- Unter dem Motiv abwechselnd einen hellen und einen dunklen Hilfshintergrund
  einblenden. Besonders die dunkelblaue Außenkante muss erkennbar bleiben.
- Falls die Kante dunkel verschwindet, die Farbe der Grundform behutsam aufhellen
  oder eine schmale, feste Kontur erproben. Keinen äußeren Leuchtschein hinzufügen.
- Bei 32, 64, 128 und 256 Pixeln in tatsächlicher Anzeigegröße prüfen, ob die
  Landschaft weiterhin erkennbar ist.

## 5. Den Master exportieren und Plattformdateien erzeugen

Blende **Raster und alle Hilfshintergründe aus**. Die Ebenen-Arbeitsdatei als PDN
speichern und den fertigen Master als **1024 × 1024 PNG mit Transparenz** nach
`bingwalle_icon_1024.png` exportieren. Die eigenen Exporte für Windows/Linux
liegen daneben als `bingwalle_icon_32.png`, `bingwalle_icon_64.png`,
`bingwalle_icon_128.png` und `bingwalle_icon_256.png`.
Das Schachbrett darf nicht ins Bild gerendert werden.

Die abweichende Apple-Vorlage separat als **1024 × 1024 PNG** unter
`bingwalle_icon_apple.png` speichern. Sie wird ausschließlich unter macOS angezeigt.
Sie darf auch ein vollständig deckendes RGB-Bild sein.

Flathub nennt SVG oder eine quadratische PNG ab 256 × 256 Pixeln als Format.
Unser 1024er Master liefert genügend Auflösung für die bestehenden Plattformdateien.
Im Projektordner anschließend ausführen, wenn Python und Pillow installiert sind:

```bash
python scripts/generate-icons.py
```

Das vorhandene Skript erzeugt daraus:

| Datei | Verwendung |
| --- | --- |
| `branding/bingwalle.png` | Desktop-Master für README und Branding |
| `BingWallE/Assets/bingwalle.png` | 256er PNG für Linux, Flatpak und Windows-/Linux-Anwendungskopf |
| `BingWallE/Assets/bingwalle-logo.png` | 1024er PNG für den Windows-/Linux-About-Dialog |
| `BingWallE/Assets/bingwalle.ico` | Windows, mehrere Größen |
| `BingWallE/Assets/bingwalle.icns` | macOS-Bundle, Dock und Finder, aus der Apple-Vorlage |
| `BingWallE/Assets/bingwalle-apple.png` | macOS-Anwendungskopf und Fenster-Icon |
| `BingWallE/Assets/bingwalle-apple-logo.png` | macOS-About-Dialog |

Das Skript überschreibt keine PNG-Vorlagen im Projektordner. Die Windows-ICO
wird automatisch mit 16, 24, 32, 48, 64, 128 und 256 Pixeln erzeugt; ein gesonderter
ICO-Upload ist nicht nötig. Die Apple-ICNS enthält die macOS-/Retina-Größen.

Danach Titelleiste, About-Dialog, Linux-Menü und Store-Vorschau kontrollieren.
Das Skript konvertiert und skaliert das Motiv; es korrigiert keinen zu großen
Fußabdruck. Änderungen werden in installierten Apps und Stores erst durch die
entsprechenden neuen Pakete beziehungsweise Store-Aktualisierungen sichtbar.

## Quellen

- [Flathub: App Icon](https://docs.flathub.org/docs/for-app-authors/metainfo-guidelines/quality-guidelines#app-icon)
- [Flathub: offizielle SVG-Rastervorlage](https://docs.flathub.org/img/app-icon-grid.svg)

Die Rasterdatei im Projekt ist eine unveränderte Referenz von Flathub. Sie dient
ausschließlich zum Ausrichten und ist kein Bestandteil des ausgelieferten Icons.
