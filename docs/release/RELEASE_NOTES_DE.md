# Release Notes V2.3.0 (für Ahnenforschung)

Diese Version bringt die größten Verbesserungen bei der automatischen Erkennung seit langem: eine deutlich zuverlässigere Gesichtserkennung, eine Reihenerkennung, die Reihen so gruppiert, wie ein Mensch sie zählen würde, und eine aufgeräumte Bedienung mit klassischem Datei-Menü. Dazu gibt es jetzt ein Online-Handbuch.

## Neue Funktionen

- **Datei-Menü statt Button-Spalte**
  - Alle Dateiaktionen an einem Ort: **Öffnen**, **Speichern** (Strg+S), **Speichern unter...** (JPG oder PDF in einem Dialog) und **Metadaten exportieren...**
  - Die **Einstellungen** finden sich jetzt unter Datei → Einstellungen; die Bildvorschau hat dadurch mehr Platz.

- **Online-Handbuch**
  - Das vollständige Handbuch (deutsch und englisch, mit Changelog) steht jetzt unter [autonumber.niggl-schlagbauer.de](https://autonumber.niggl-schlagbauer.de/) bereit.
  - Das neue **Hilfe-Menü** in der App führt direkt dorthin.

- **Zuverlässigere Gesichtserkennung**
  - Umstellung auf ein modernes DNN-Verfahren (YuNet): erkennt Gesichter auf alten und gescannten Fotos deutlich besser — auch seitliche, kleine oder unscharfe Gesichter.

- **Deutlich bessere automatische Reihenerkennung**
  - Reihen werden jetzt so gruppiert, wie man sie auf dem Foto tatsächlich zählen würde — auch bei schiefem Kamerawinkel, Kindern zwischen Erwachsenen oder sitzenden und stehenden Reihen.
  - Die Reihengrenzen folgen als schräge, parallele Linien der Neigung des Fotos und passen von Anfang an; Feinabstimmung im Reihenmodus funktioniert wie gewohnt.

- **Bessere Nummern-Platzierung**
  - Die Größe der Nummern richtet sich bei neuen Bildern wieder nach der erkannten Gesichtsgröße.
  - Neuer wählbarer **Anker** (3×3-Raster): bestimmt, wo neu erkannte Nummern relativ zum Gesicht sitzen — als App-Standard in den Einstellungen und pro Bild im Formatieren-Dialog.
  - **Strg+Ziehen** verschiebt alle Nummern gemeinsam.

- **Komfort in der Namensliste**
  - Bewegt man die Maus über einen Eintrag, wird die zugehörige Nummer im Bild hervorgehoben — ideal, um Namen und Gesicht schnell zuzuordnen.
  - Die Schriftgröße von Namensliste, Überschrift, Beschreibung und Bild-ID ist jetzt unabhängig von der Nummerngröße.

## Verbesserungen

- Der **Öffnen-Dialog merkt sich den Ordner der Originalfotos**, unabhängig davon, wohin gespeichert wurde — praktisch beim Abarbeiten ganzer Foto-Ordner.
- Der **Einstellungen-Dialog** hat jetzt **OK und Abbrechen**: Änderungen werden erst mit OK übernommen.
- Öffnen und Drehen großer Fotos ist **deutlich schneller**; das Speichern großer Scans belegt nicht mehr kurzzeitig sehr viel Arbeitsspeicher.
- Flüssigeres Arbeiten bei Fotos mit vielen Personen.

## Fehlerbehebungen

- „Speichern unter..." hängt einen relativen Ausgabeordner nicht mehr bei jedem Speichern erneut an; relative Ausgabeordner funktionieren jetzt generell zuverlässig, und bei unbrauchbarem Ausgabeordner erscheint eine Meldung.
- Graustufen-JPEGs ließen die Gesichtserkennung abstürzen.
- Die gespeicherte Nummernposition war um den halben Nummerndurchmesser versetzt; alte Dateien werden beim Öffnen automatisch korrigiert.
- Fehler beim Öffnen eines Bildes werden nicht mehr verschluckt, sondern angezeigt; Speichern ohne geladenes Bild meldet sich jetzt ebenfalls.
- PDF: Das Bild verschwand in Acrobat DC beim Scrollen bei 100 % Zoom; außerdem einen ungültigen Eintrag im PDF-Dateiverzeichnis behoben.
- Die Anker-Auswahl in den Erkennungs-Einstellungen sprang teils auf die vorherige Auswahl zurück.

---

Vielen Dank fürs Nutzen von AutoNum bei Ihrer Ahnenforschung. Wir wünschen viel Erfolg beim Beschriften und Dokumentieren Ihrer Familienfotos.
