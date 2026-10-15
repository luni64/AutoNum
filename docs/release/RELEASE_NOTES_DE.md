# Release Notes V2.5.0 (für Ahnenforschung)

Diese Version erfüllt einen Wunsch aus dem Forum: Die Namensliste kann jetzt rechts neben dem Foto stehen statt darunter. Außerdem behebt sie einen Absturz beim Speichern, der bei Fotos mit umfangreichen Kameradaten auftreten konnte, und AutoNumber führt ab jetzt eine Protokolldatei, die bei Problemen die Fehlersuche erheblich erleichtert.

## Neue Funktionen

- **Namensliste rechts neben dem Bild**
  - Bisher stand die Namensliste immer unter dem Foto. Neu lässt sie sich **rechts daneben** platzieren – praktisch bei Hochformat-Fotos oder wenn das Ergebnis eher breit als hoch werden soll.
  - **Pro Bild** einstellbar im Formatieren-Dialog der Namensliste (Zahnrad neben der Namensliste): Position „Unter dem Bild" oder „Rechts neben dem Bild" sowie die **Breite** der seitlichen Liste (15–100 % der Bildbreite, Standard 50 %).
  - Als **Standard für neue Bilder**: über „Als Standard übernehmen" im selben Dialog oder unter Datei → Einstellungen → Formatierung.
  - Überschrift und Beschreibung erstrecken sich dann über Bild und Liste, die Bild-ID bleibt unter dem Bild. Ist die Liste länger als das Bild hoch ist, wird das gespeicherte Bild entsprechend höher.
  - Die Position wird **im Bild mitgespeichert** und beim erneuten Öffnen wiederhergestellt.

- **Protokolldatei**
  - AutoNumber hält die wichtigsten Schritte fest: geöffnete und gespeicherte Dateien, gefundene AutoNum-Daten und deren Version, erkannte Gesichter, Warnungen und Fehler. Die Datei enthält keine Bildinhalte oder Namen, nur Dateinamen und technische Angaben.
  - Standard-Speicherort ist `%AppData%\AutoNum\autonum.log`; über **Hilfe → Protokolldatei öffnen** wird sie im Texteditor geöffnet. Bei Problemen fügen Sie diese Datei bitte Ihrer Fehlermeldung bei.
  - Im neuen Reiter **Protokoll** der Einstellungen lässt sich das Protokoll abschalten oder der Speicherort ändern.

## Verbesserungen

- Die Formatieren-Dialoge (Nummern, Namensliste) sind übersichtlicher gestaltet: Abschnitte mit Überschriften, sauber ausgerichtete Felder, keine leeren Flächen mehr.
- Der Einstellungsdialog ist etwas höher, sodass der Reiter „Erkennung" ohne Scrollen vollständig sichtbar ist; die Reiterleiste hat keinen weißen Hintergrund mehr.

## Fehlerbehebungen

- **Absturz beim Speichern als JPG** bei Fotos mit umfangreichen EXIF-Daten (z. B. einem großen eingebetteten Vorschaubild der Kamera). Die AutoNum-Daten werden jetzt in einem eigenen Abschnitt der JPEG-Datei abgelegt statt im EXIF-Feld „UserComment". Dadurch bleiben die EXIF-Daten des Originals vollständig erhalten, und Programme wie digiKam, die das UserComment-Feld mit ihrer eigenen Bildbeschreibung überschreiben, löschen die AutoNum-Daten nicht mehr.
- Ein Fehler des JPEG-Encoders beim Speichern führt nicht mehr zum Absturz, sondern zu einer Fehlermeldung.
- Unerwartete Fehler beenden das Programm nicht mehr stillschweigend: Es erscheint eine Fehlermeldung, und die Details werden in der Protokolldatei festgehalten.

## Hinweise für bestehende Dateien

- Mit älteren Versionen gespeicherte Bilder werden weiterhin geöffnet und können wie gewohnt bearbeitet werden.
- Umgekehrt können **ältere AutoNumber-Versionen** ein mit V2.5 gespeichertes Bild nur noch als normales Foto öffnen, weil sie die AutoNum-Daten an der neuen Stelle nicht finden. Nach dem Update auf V2.5 ist das ohne Bedeutung.
- Bearbeitet man ein von AutoNumber gespeichertes JPG in einem Bildbearbeitungsprogramm und speichert es dort neu, gehen die AutoNum-Daten – wie bisher auch – verloren.

---

Vielen Dank fürs Nutzen von AutoNum bei Ihrer Ahnenforschung. Wir wünschen viel Erfolg beim Beschriften und Dokumentieren Ihrer Familienfotos.
