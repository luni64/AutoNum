# Next Release

## Features

- Namensliste wahlweise rechts neben dem Bild statt darunter (Anregung aus dem Forum, GitHub #21): Position und Breite der seitlichen Liste (15–100 % der Bildbreite) lassen sich im Formatierungsdialog der Namensliste je Bild einstellen und als Standard übernehmen; der Standard für neue Bilder steht auch im Reiter „Formatierung“ der Einstellungen. Überschrift und Beschreibung erstrecken sich dann über Bild und Liste, die Bild-ID bleibt unter dem Bild.

- Protokolldatei: AutoNumber schreibt jetzt die wichtigsten Schritte (geöffnete und gespeicherte Dateien, gefundene AutoNum-Daten und deren Version, erkannte Gesichter, Warnungen und Fehler) in eine Protokolldatei, standardmäßig `%AppData%\AutoNum\autonum.log`. Bei Problemen bitte diese Datei der Fehlermeldung beifügen. Neu dazu: „Hilfe → Protokolldatei öffnen“ und der Reiter „Protokoll“ in den Einstellungen, in dem sich das Protokoll abschalten und der Speicherort ändern lässt.

## Bug Fixes

- Absturz beim Speichern als JPG, wenn das Originalbild umfangreiche EXIF-Daten enthält (z. B. ein großes eingebettetes Vorschaubild): Die AutoNum-Daten werden jetzt in einem eigenen JPEG-Segment (APP4) abgelegt statt im EXIF-Feld „UserComment“. Dadurch bleiben die EXIF-Daten des Originals vollständig erhalten, und Programme wie digiKam, die das UserComment-Feld überschreiben, löschen die AutoNum-Daten nicht mehr. Mit älteren AutoNum-Versionen gespeicherte Bilder werden weiterhin geöffnet; umgekehrt können ältere Versionen neu gespeicherte Bilder nicht mehr bearbeiten.
- Ein Fehler des JPEG-Encoders beim Speichern führt nicht mehr zum Absturz, sondern zu einer Fehlermeldung.
- Unerwartete Fehler beenden das Programm nicht mehr stillschweigend: Es erscheint eine Fehlermeldung, und die Details werden in der Protokolldatei festgehalten.
