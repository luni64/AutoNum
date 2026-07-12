# Changelog

Alle wesentlichen Änderungen an AutoNumber werden in dieser Datei dokumentiert. Das Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/); die Versionen entsprechen den getaggten [GitHub-Releases](https://github.com/luni64/AutoNum/releases).

## V2.3.0 - 2026-07-12

**Neu**

- Datei-Menü ersetzt die bisherigen Öffnen-/Speichern-Buttons und das Einstellungen-Zahnrad: Öffnen, Speichern (Strg+S), Speichern unter... (JPG oder PDF in einem Dialog), Metadaten exportieren... und Datei → Einstellungen; die Bildvorschau hat dadurch mehr Platz.
- Neues Hilfe-Menü mit Link zum Online-Handbuch unter [autonumber.niggl-schlagbauer.de](https://autonumber.niggl-schlagbauer.de/).
- Gesichtserkennung auf ein modernes DNN-Verfahren (YuNet) umgestellt — deutlich zuverlässiger auf alten und gescannten Fotos.
- Deutlich verbesserte automatische Reihenerkennung: Reihen werden wie von einem Menschen gezählt gruppiert, die Reihengrenzen folgen als schräge, parallele Linien der Neigung des Fotos.
- Die Größe der Nummern richtet sich bei neuen Bildern wieder nach der erkannten Gesichtsgröße statt nur nach der Bildbreite.
- Wählbarer Anker für neu erkannte Nummern relativ zum Gesicht (3×3-Raster), als App-Standard in den Einstellungen und pro Bild im Formatieren-Dialog.
- Bewegt man die Maus über einen Eintrag der Namensliste, wird die zugehörige Nummer im Bild hervorgehoben.
- Strg+Ziehen verschiebt alle Nummern gemeinsam.
- Die Schriftgröße von Namensliste, Überschrift, Beschreibung und Bild-ID ist unabhängig von der Nummerngröße einstellbar.

**Geändert**

- Der Öffnen-Dialog merkt sich den Ordner der Originalfotos, unabhängig davon, wohin gespeichert wurde.
- Der Einstellungen-Dialog hat jetzt OK und Abbrechen: Änderungen werden erst mit OK übernommen und gespeichert.
- Öffnen und Drehen großer Fotos ist deutlich schneller; das Speichern großer Scans belegt nicht mehr kurzzeitig sehr viel Arbeitsspeicher.
- Flüssigeres Arbeiten bei Fotos mit vielen Personen (Hervorheben, Ziehen von Reihengrenzen).
- Die Version im Fenstertitel kommt aus der Projektdatei und kann bei Releases nicht mehr vergessen werden.

**Behoben**

- „Speichern unter..." hängt einen relativen Ausgabeordner nicht mehr bei jedem Speichern erneut an; relative Ausgabeordner funktionieren jetzt generell zuverlässig, und bei unbrauchbarem Ausgabeordner erscheint eine Meldung statt eines stillen Ausweichens.
- Graustufen-JPEGs ließen die Gesichtserkennung abstürzen.
- Die gespeicherte Nummernposition war um den halben Nummerndurchmesser versetzt; alte Dateien werden beim Öffnen automatisch korrigiert.
- Fehler beim Öffnen eines Bildes werden nicht mehr verschluckt, sondern angezeigt; Speichern ohne geladenes Bild meldet sich jetzt ebenfalls.
- PDF: Das Bild verschwand in Acrobat DC beim Scrollen bei 100 % Zoom; außerdem einen ungültigen Eintrag im PDF-Dateiverzeichnis behoben.
- Die Anker-Auswahl in den Erkennungs-Einstellungen sprang teils auf die vorherige Auswahl zurück.

## V2.2.0 - 2026-07-06

**Neu**
- Automatische Reihenerkennung und -verwaltung: Reihen können automatisch erkannt, manuell bearbeitet und flexibel zurückgesetzt werden.
- Neuer Button „Zoom auf Bild", der im Reihenmodus Bild und Reihen-Bedienelemente gemeinsam in den sichtbaren Bereich einpasst.
- Neuer Bereich „Erkennung" in den Einstellungen für Gesichtserkennung und Reihenerkennung.
- Gesichtserkennung und Reihenerkennung lassen sich für neue Bilder unabhängig voneinander ein- und ausschalten.
- Erkennungsparameter können direkt in den Einstellungen angepasst und mit einem Klick erneut angewendet werden, inklusive Zurücksetzen auf die Standardwerte.
- Die Formatierung der Nummern kann als Standard gespeichert werden.

**Geändert**
- Namensliste und Reihenbearbeitung wurden optisch überarbeitet und sind einfacher zu bedienen.
- Die Bedienelemente des Reihenmodus wurden vereinfacht: Das Eingabefeld für die Reihenanzahl entfällt, und der Reihenmodus-Schalter befindet sich jetzt in der Gruppe „Nummerierung".
- Das Standardlayout für neue Bilder wurde überarbeitet und ist kompakter.
- Bearbeitbare PDF-Dateien werden als standardkonforme PDF-Anhänge gespeichert und können wieder geöffnet werden.
- Viele Symbole der Werkzeugleiste wurden durch einen einheitlicheren Symbolsatz ersetzt.

**Behoben**
- AutoNum fragt jetzt mit „Wiederholen/Abbrechen" nach, wenn die zu speichernde Datei gerade in einem anderen Programm geöffnet ist.
- Der sichtbare Bildausschnitt wird beim erneuten Öffnen eines gespeicherten Bildes wieder korrekt hergestellt.
- Die Zuordnung von Personen zu Reihen funktioniert jetzt auch dann korrekt, wenn Reihengrenzen schräg gezogen wurden.
- Die Namensliste im PDF verwendet eine schmalere Nummernspalte.
- Lange Überschriften und Bildinformationen werden umbrochen statt abgeschnitten.

## V2.1.0 - 2026-07-01

**Neu**
- Neuer Button „Jetzt exportieren" unter Einstellungen → Export für den direkten Metadaten-Export.

**Behoben**
- Der PDF-Export berücksichtigt jetzt wie der JPG-Export die Metadaten-Optionen unter Einstellungen → Export (CSV-/JSON-Begleitdateien).
- Zuvor eingegebene bildbezogene Texte (Überschrift, Bildinformationen, Bild-ID) werden beim Öffnen eines neuen Bildes korrekt zurückgesetzt.

## V2.0.0 - 2026-06-30

**Neu**
- Neue Bild-Aktionsleiste unter der Vorschau: Drehen um 90°, Zoom auf Inhalt anpassen.
- Getrennte Speichern-Buttons: „Als JPG speichern" und „Als PDF speichern" (bleibt bearbeitbar).
- Metadaten-Export: Überschrift, Beschreibung, Bild-ID und Namensliste können als `.csv` oder `.json` exportiert werden, z. B. zur Weiterverarbeitung in Excel oder Datenbanken.
- PDFs wieder öffnen: Mit AutoNumber erstellte PDF-Dateien lassen sich wie JPGs erneut öffnen und weiterbearbeiten.
- Mehr Kontrolle über die Namensliste: Die Spaltenanzahl ist pro Bild einstellbar (1–4), z. B. für kompakte Listen bei größeren Gruppenfotos.
- Eigene Felder für Bildinformationen und Bild-ID, getrennt von der Überschrift, jeweils unabhängig ein-/ausblendbar und formatierbar.

**Geändert**
- Einheitlicher Formatierungsdialog für Überschrift, Bildinformationen, Bild-ID und Namensliste mit klarerer Prozentanzeige für Größen.
- Überarbeitetes Einstellungsfenster mit klareren Bereichen für Schriften, Erkennung und Speichern; Standardwerte lassen sich einfacher übernehmen.
- Größen, Schriftarten und Sichtbarkeit werden beim erneuten Öffnen einer Datei zuverlässiger wiederhergestellt.
- Verbesserte Darstellungsqualität für kleine Nummern und Namenslisten im JPG- und PDF-Export.
- AutoNum-Dateien benötigen zum Bearbeiten nicht mehr das Originalbild — alle nötigen Informationen sind direkt in den Metadaten gespeichert.

## V1.3.0 - 2025-10-25

**Behoben**
- Beim Öffnen von mit AutoNum erstellten Bildern wurde die Nummerngröße falsch (zu klein) gesetzt.
- Das Öffnen von mit AutoNum erstellten Bildern löste eine automatische Neunummerierung aus und veränderte damit manuell angepasste Nummerierungen.

## V1.2.1 - 2025-03-10

**Neu**
- Beim Öffnen eines nummerierten Bildes, dessen Original nicht automatisch gefunden wird, kann jetzt danach gesucht werden.

**Geändert**
- Umstellung auf `Bitmap.Open` statt der OpenCV-Lademethode; die unterstützten Eingabeformate reduzieren sich dadurch auf JPG, PNG, TIFF, BMP, GIF und EXIF.
- Der Export ist vorübergehend auf JPG beschränkt (wegen der Metadaten-Verarbeitung).

**Behoben**
- Beim Öffnen eines nummerierten Bildes war die Nummerngröße falsch.
- Abstürze beim Öffnen von Nicht-Bilddateien behoben.

## V1.2.0 - 2025-03-05

**Neu**
- Beta-Unterstützung für das Öffnen und Bearbeiten bereits nummerierter Bilder, z. B. um nachträglich identifizierte Personen in einem vorhandenen Bild zu ergänzen.

> **Hinweis:** AutoNumber benötigt in dieser Version zum Bearbeiten eines nummerierten Bildes das Originalbild; dessen Speicherort wird beim Speichern in den Metadaten abgelegt. Mit älteren Versionen erstellte Bilder ohne diese Metadaten können nicht bearbeitet werden.

## V1.1.15 - 2025-03-03

**Geändert**
- Neue Oberfläche und neue Funktionen (erste dokumentierte Version).
