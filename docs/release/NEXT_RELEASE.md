# Next Release

## Features

- Protokolldatei: AutoNumber schreibt jetzt die wichtigsten Schritte (geöffnete und gespeicherte Dateien, gefundene AutoNum-Daten und deren Version, erkannte Gesichter, Warnungen und Fehler) nach `%AppData%\AutoNum\autonum.log`. Bei Problemen bitte diese Datei der Fehlermeldung beifügen.

## Bug Fixes

- Absturz beim Speichern als JPG, wenn das Originalbild umfangreiche EXIF-Daten enthält (z. B. ein großes eingebettetes Vorschaubild): Die AutoNum-Daten werden jetzt in einem eigenen JPEG-Segment (APP4) abgelegt statt im EXIF-Feld „UserComment“. Dadurch bleiben die EXIF-Daten des Originals vollständig erhalten, und Programme wie digiKam, die das UserComment-Feld überschreiben, löschen die AutoNum-Daten nicht mehr. Mit älteren AutoNum-Versionen gespeicherte Bilder werden weiterhin geöffnet; umgekehrt können ältere Versionen neu gespeicherte Bilder nicht mehr bearbeiten.
- Ein Fehler des JPEG-Encoders beim Speichern führt nicht mehr zum Absturz, sondern zu einer Fehlermeldung.
- Unerwartete Fehler beenden das Programm nicht mehr stillschweigend: Es erscheint eine Fehlermeldung, und die Details werden in der Protokolldatei festgehalten.
