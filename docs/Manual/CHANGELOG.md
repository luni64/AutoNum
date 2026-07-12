# Changelog

Alle wesentlichen Änderungen an AutoNumber werden in dieser Datei dokumentiert. Das Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/); die Versionen entsprechen den getaggten [GitHub-Releases](https://github.com/luni64/AutoNum/releases).

## V2.2.0 - 2026-07-06

### Neu
- Automatische Reihenerkennung und -verwaltung: Reihen können automatisch erkannt, manuell bearbeitet und flexibel zurückgesetzt werden.
- Neuer Button „Zoom auf Bild", der im Reihenmodus Bild und Reihen-Bedienelemente gemeinsam in den sichtbaren Bereich einpasst.
- Neuer Bereich „Erkennung" in den Einstellungen für Gesichtserkennung und Reihenerkennung.
- Gesichtserkennung und Reihenerkennung lassen sich für neue Bilder unabhängig voneinander ein- und ausschalten.
- Erkennungsparameter können direkt in den Einstellungen angepasst und mit einem Klick erneut angewendet werden, inklusive Zurücksetzen auf die Standardwerte.
- Die Formatierung der Nummern kann als Standard gespeichert werden.

### Geändert
- Namensliste und Reihenbearbeitung wurden optisch überarbeitet und sind einfacher zu bedienen.
- Die Bedienelemente des Reihenmodus wurden vereinfacht: Das Eingabefeld für die Reihenanzahl entfällt, und der Reihenmodus-Schalter befindet sich jetzt in der Gruppe „Nummerierung".
- Das Standardlayout für neue Bilder wurde überarbeitet und ist kompakter.
- Bearbeitbare PDF-Dateien werden als standardkonforme PDF-Anhänge gespeichert und können wieder geöffnet werden.
- Viele Symbole der Werkzeugleiste wurden durch einen einheitlicheren Symbolsatz ersetzt.

### Behoben
- AutoNum fragt jetzt mit „Wiederholen/Abbrechen" nach, wenn die zu speichernde Datei gerade in einem anderen Programm geöffnet ist.
- Der sichtbare Bildausschnitt wird beim erneuten Öffnen eines gespeicherten Bildes wieder korrekt hergestellt.
- Die Zuordnung von Personen zu Reihen funktioniert jetzt auch dann korrekt, wenn Reihengrenzen schräg gezogen wurden.
- Die Namensliste im PDF verwendet eine schmalere Nummernspalte.
- Lange Überschriften und Bildinformationen werden umbrochen statt abgeschnitten.

## V2.1.0 - 2026-07-01

### Neu
- Neuer Button „Jetzt exportieren" unter Einstellungen → Export für den direkten Metadaten-Export.

### Behoben
- Der PDF-Export berücksichtigt jetzt wie der JPG-Export die Metadaten-Optionen unter Einstellungen → Export (CSV-/JSON-Begleitdateien).
- Zuvor eingegebene bildbezogene Texte (Überschrift, Bildinformationen, Bild-ID) werden beim Öffnen eines neuen Bildes korrekt zurückgesetzt.

## V2.0.0 - 2026-06-30

### Neu
- Neue Bild-Aktionsleiste unter der Vorschau: Drehen um 90°, Zoom auf Inhalt anpassen.
- Getrennte Speichern-Buttons: „Als JPG speichern" und „Als PDF speichern" (bleibt bearbeitbar).
- Metadaten-Export: Überschrift, Beschreibung, Bild-ID und Namensliste können als `.csv` oder `.json` exportiert werden, z. B. zur Weiterverarbeitung in Excel oder Datenbanken.
- PDFs wieder öffnen: Mit AutoNumber erstellte PDF-Dateien lassen sich wie JPGs erneut öffnen und weiterbearbeiten.
- Mehr Kontrolle über die Namensliste: Die Spaltenanzahl ist pro Bild einstellbar (1–4), z. B. für kompakte Listen bei größeren Gruppenfotos.
- Eigene Felder für Bildinformationen und Bild-ID, getrennt von der Überschrift, jeweils unabhängig ein-/ausblendbar und formatierbar.

### Geändert
- Einheitlicher Formatierungsdialog für Überschrift, Bildinformationen, Bild-ID und Namensliste mit klarerer Prozentanzeige für Größen.
- Überarbeitetes Einstellungsfenster mit klareren Bereichen für Schriften, Erkennung und Speichern; Standardwerte lassen sich einfacher übernehmen.
- Größen, Schriftarten und Sichtbarkeit werden beim erneuten Öffnen einer Datei zuverlässiger wiederhergestellt.
- Verbesserte Darstellungsqualität für kleine Nummern und Namenslisten im JPG- und PDF-Export.
- AutoNum-Dateien benötigen zum Bearbeiten nicht mehr das Originalbild — alle nötigen Informationen sind direkt in den Metadaten gespeichert.

## V1.3.0 - 2025-10-25

### Behoben
- Beim Öffnen von mit AutoNum erstellten Bildern wurde die Nummerngröße falsch (zu klein) gesetzt.
- Das Öffnen von mit AutoNum erstellten Bildern löste eine automatische Neunummerierung aus und veränderte damit manuell angepasste Nummerierungen.

## V1.2.1 - 2025-03-10

### Neu
- Beim Öffnen eines nummerierten Bildes, dessen Original nicht automatisch gefunden wird, kann jetzt danach gesucht werden.

### Geändert
- Umstellung auf `Bitmap.Open` statt der OpenCV-Lademethode; die unterstützten Eingabeformate reduzieren sich dadurch auf JPG, PNG, TIFF, BMP, GIF und EXIF.
- Der Export ist vorübergehend auf JPG beschränkt (wegen der Metadaten-Verarbeitung).

### Behoben
- Beim Öffnen eines nummerierten Bildes war die Nummerngröße falsch.
- Abstürze beim Öffnen von Nicht-Bilddateien behoben.

## V1.2.0 - 2025-03-05

### Neu
- Beta-Unterstützung für das Öffnen und Bearbeiten bereits nummerierter Bilder, z. B. um nachträglich identifizierte Personen in einem vorhandenen Bild zu ergänzen.

> **Hinweis:** AutoNumber benötigt in dieser Version zum Bearbeiten eines nummerierten Bildes das Originalbild; dessen Speicherort wird beim Speichern in den Metadaten abgelegt. Mit älteren Versionen erstellte Bilder ohne diese Metadaten können nicht bearbeitet werden.

## V1.1.15 - 2025-03-03

### Geändert
- Neue Oberfläche und neue Funktionen (erste dokumentierte Version).
