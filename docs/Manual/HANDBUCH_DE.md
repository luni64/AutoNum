# AutoNumber – Benutzerhandbuch

*[English version](MANUAL_EN.md)*

## Inhaltsverzeichnis

1. [Einführung](#1-einführung)
2. [Oberfläche im Überblick](#2-oberfläche-im-überblick)
3. [Schnellstart](#3-schnellstart)
4. [Bild öffnen](#4-bild-öffnen)
5. [Werkzeugleiste](#5-werkzeugleiste)
6. [Gesichter & Nummerierung](#6-gesichter--nummerierung)
7. [Namensliste](#7-namensliste)
8. [Überschrift, Beschreibung, Bild-ID](#8-überschrift-beschreibung-bild-id)
9. [Speichern & Export](#9-speichern--export)
10. [Einstellungen](#10-einstellungen)
11. [CSV/JSON-Metadaten-Export](#11-csvjson-metadaten-export)
12. [Tipps für die Ahnenforschung](#12-tipps-für-die-ahnenforschung)
13. [Häufige Fragen (FAQ)](#13-häufige-fragen-faq)
14. [Installation](#14-installation)
15. [Anhang](#15-anhang)

---

## 1. Einführung

AutoNumber hilft Ihnen dabei,

- Gesichter auf Fotos automatisch zu erkennen und mit Nummern zu versehen,
- Nummern manuell direkt auf dem Bild zu platzieren,
- eine passende Namensliste zu pflegen,
- und das Ergebnis als **JPG** oder **bearbeitbares PDF** zu speichern.

So können Sie Familienfotos, Klassenbilder oder Gruppenaufnahmen schnell aufbereiten.

---

## 2. Oberfläche im Überblick

Die Oberfläche besteht aus drei Bereichen:

1. **Links: Datei-Bereich**
   - Bild öffnen
   - Als JPG speichern
   - Als PDF speichern
   - Kurzhinweise zur Bedienung

2. **Mitte: Bildvorschau**
   - Hier sehen Sie das Foto mit Nummern und Zusatztexten.
   - Unten befinden sich Aktionsgruppen **Bild** (Drehen, Zoom) und **Nummerierung** (Reihenmodus, Alle löschen, Gesichter neu erkennen, Formatieren).

3. **Rechts: Text- und Listenbereich**
   - Überschrift
   - Beschreibung/Bildinformation
   - Bild-ID
   - Namensliste (Nr. / Name)

Jeder Block rechts hat ein Augen-Symbol zum Ein-/Ausblenden und ein Zahnrad-Symbol zum Formatieren.

![Hauptfenster – Gesamtansicht](Pictures/02_oberflaeche_gesamt.png)
<!-- Zeigt: leeres oder gefülltes Hauptfenster, mit Beschriftung/Pfeilen auf die drei Bereiche (Datei-Bereich, Bildvorschau, Text-/Listenbereich) -->

---

## 3. Schnellstart

1. **Bild öffnen** (linke Spalte).
2. Bei Bedarf das Bild mit **Drehen** (Button unter der Vorschau) korrekt ausrichten.
3. Gesichter werden automatisch erkannt und nummeriert.
4. In der **Namensliste** rechts die Namen zu den Nummern eintragen.
5. Optional Überschrift, Beschreibung und Bild-ID ergänzen.
6. Das Ergebnis als **JPG** oder **PDF** speichern.

---

## 4. Bild öffnen

- Klicken Sie links auf **Bild öffnen**.
- Unterstützte Formate: JPG, PDF, PNG, TIFF, BMP, GIF.
- Nach dem Öffnen startet die Gesichtserkennung automatisch (sofern in den Einstellungen aktiviert).
- Bereits mit AutoNumber bearbeitete JPG- oder PDF-Dateien können ebenfalls hier geöffnet werden – Layout, Sichtbarkeit und Schriftgrößen werden dabei wiederhergestellt (siehe [Kapitel 9](#9-speichern--export)).

![Bild öffnen](Pictures/04_bild_oeffnen.png)
<!-- Zeigt: Datei-Bereich links mit "Bild öffnen"-Button und Format-Hinweis -->

---

## 5. Werkzeugleiste

Unter der Bildvorschau befinden sich zwei Werkzeuggruppen: **Bild** (Zoom und Drehen) und **Nummerierung** (Reihenmodus, Alle löschen, Gesichter neu erkennen, Formatieren). Dieses Kapitel behandelt die Gruppe **Bild**; die Nummerierungs-Werkzeuge werden ausführlich in [Kapitel 6](#6-gesichter--nummerierung) erklärt.

![Werkzeuge unter der Bildvorschau](Pictures/05_werkzeugleiste.png)
<!-- Zeigt: die Bild- und Nummerierungs-Werkzeugleiste unter der Vorschau, alle Icons sichtbar -->

### 5.1 Navigation in der Bildvorschau

- **Mausrad:** Ein-/Auszoomen
- **Linke Maustaste (ziehen):** Bild verschieben
- **Zoom auf Bild einpassen:** passt das ganze Bild in den sichtbaren Bereich ein
- **Zoom auf Bild:** zoomt auf 100 % der Bildauflösung

Tipp: Wenn die Ansicht „verrutscht“ ist, hilft meist ein Klick auf **Zoom auf Bild einpassen**.

### 5.2 Bild drehen

- Nutzen Sie den **Drehen-Button** in der Gruppe „Bild“ unter der Bildvorschau.
- Jeder Klick dreht das Bild um **90° im Uhrzeigersinn**.

Wichtig:
- Wenn bereits Namen in der Namensliste eingetragen sind, erscheint vorher eine Sicherheitsabfrage.
- Nach dem Drehen wird das Bild neu ausgewertet (Gesichtserkennung läuft erneut, sofern aktiviert).

---

## 6. Gesichter & Nummerierung

Die Werkzeuggruppe **Nummerierung** bietet vier Werkzeuge: Reihenmodus, Alle löschen, Gesichter neu erkennen und Formatieren. Sie werden im Folgenden im Detail erklärt.

### 6.1 Automatische Erkennung

Wenn die automatische Gesichtserkennung aktiviert ist (siehe [Kapitel 10](#10-einstellungen)), erkennt AutoNumber beim Öffnen eines neuen Bildes Gesichter automatisch und setzt Nummern-Label. Ist zusätzlich die Reihenerkennung aktiv, versucht AutoNumber, die Personen reihenweise zu nummerieren.

- **Gesichter neu erkennen:** Startet die automatische Erkennung erneut auf dem aktuellen Bild. Bitte beachten Sie, dass eine erneute Erkennung alle bestehenden Nummern-Labels und Namen entfernt – die Namen müssen anschließend erneut zugeordnet werden.
- **Alle löschen:** Entfernt alle aktuell gesetzten Nummern-Label und die zugehörigen Namen. Sinnvoll, wenn die automatische Erkennung ein unpassendes Ergebnis geliefert hat und Sie lieber sauber manuell starten möchten.

**Wichtiger Hinweis:** Wenn bereits Namen eingetragen sind, erscheint bei **Alle löschen** und **Gesichter neu erkennen** eine Sicherheitsabfrage, damit keine Zuordnungen versehentlich verloren gehen.

### 6.2 Nummern setzen, verschieben und löschen

Direkt in der Bildvorschau können Sie die Nummern-Label mit der Maus bearbeiten:

- **Rechte Maustaste auf freie Stelle:** erzeugt eine neue Nummer.
- **Rechte Maustaste auf bestehende Nummer:** löscht diese Nummer; die übrigen Nummern werden automatisch neu durchgezählt.
- **Linke Maustaste auf einem Label ziehen:** verschiebt nur dieses eine Label.
- **Strg gedrückt halten und ein Label ziehen:** verschiebt alle anderen Label um denselben Versatz mit. Das ist praktisch, um die gesamte Nummerierung nach einem Zoom oder einer Drehung schnell neu auszurichten, ohne jedes Label einzeln zu verschieben.
- **Mauszeiger über einer Nummer halten:** zeigt den zugeordneten Namen als Tooltip an – praktisch, um die Namenszuordnung schnell zu prüfen, ohne in der Namensliste nachzusehen.

So können Sie auch bei schwierigen Fotos (unscharf, seitlich, teilweise verdeckt) schnell eine saubere Nummerierung aufbauen.


### 6.3 Reihenmodus

Der **Reihenmodus** (Icon mit Reihenlinien) ersetzt das manuelle Sortieren der Nummern nach Reihen: Sie fügen Reihengrenzen ein oder entfernen sie, und AutoNumber nummeriert automatisch reihenweise (erste Reihe von links nach rechts, dann die nächste usw.).

So funktioniert es:

- **Reihenmodus einschalten** (Toggle-Button). Am rechten Bildrand erscheint ein schmaler Streifen mit farbigen Abschnitten – jede Farbe steht für eine Reihe. Passend dazu werden die Reihengrenzen als farbige Linien direkt im Bild eingezeichnet, und die Nummern-Label werden entsprechend ihrer Reihe eingefärbt. So sehen Sie auf einen Blick, welche Person zu welcher Reihe gehört. Falls Sie den Streifen nicht sehen, zoomen Sie das Bild etwas heraus.

- Eine Reihe kann über die Papierkorb-Symbole rechts neben dem Streifen entfernt werden. Um eine neue Reihe einzufügen, bewegen Sie den Mauszeiger über den Streifen: An der Mausposition erscheint eine horizontale Vorschaulinie. Ein Klick an dieser Stelle fügt dort die neue Reihengrenze ein. Nach dem Löschen oder Einfügen einer Reihe werden die Label automatisch aufsteigend von links nach rechts nummeriert.

- Die Grenzlinien zwischen den Reihen lassen sich direkt im Bild per Maus anpassen:
   - **Ziehen der Linie:** verschiebt die ganze Reihengrenze parallel.
   - **Ziehen an einem Endpunkt:** kippt die Grenze (nützlich bei leicht schräg fotografierten Gruppen).

  Sobald Sie die Maustaste loslassen, werden alle Label, die durch die neue Position der Grenze auf die andere Seite gerutscht sind, automatisch der entsprechenden Reihe zugeordnet und neu nummeriert.

- Umgekehrt können Sie auch ein einzelnes Label über eine Reihengrenze ziehen: Es wird dabei sofort der neuen Reihe zugeordnet und die Nummerierung aktualisiert sich entsprechend.

Der Reihenmodus erhält die Zuordnung der Namens-Label zu den Personen. Sie müssen die Namen also nicht erneut eintragen.

Wenn Sie das Bild gar nicht in Reihen aufteilen möchten, löschen Sie im Reihenmodus einfach alle Reihen bis auf eine (eine einzelne Reihe lässt sich nicht mehr entfernen). Die Nummern werden dann schlicht von links nach rechts durchgezählt.

![Reihenmodus mit Reihengrenzen](Pictures/06c_reihenmodus.png)
<!-- Zeigt: Reihenmodus aktiv, farbiger Streifen am rechten Bildrand, 2-3 farbige Reihengrenzen direkt im Bild, Nummern-Label entsprechend ihrer Reihe eingefärbt -->

**Typischer Arbeitsweg bei schlechtem Erkennungsergebnis:**
1. „Alle löschen“
2. Nummern manuell setzen/verschieben (rechte Maustaste)
3. Bei Bedarf Reihenmodus nutzen, um die Zählreihenfolge reihenweise zu korrigieren
4. Namen in der Liste eintragen

### 6.4 Formatieren

Öffnet die Darstellungseinstellungen für die Nummern (Größe, Schriftart, Rand- und Hintergrundfarbe) – analog zu den Formatierungsdialogen von Überschrift, Beschreibung, Bild-ID und Namensliste (siehe [Kapitel 8](#8-überschrift-beschreibung-bild-id)).

---

## 7. Namensliste

Rechts im Bereich **Namensliste**:

- Spalte *Nr.* zeigt die Bildnummer (nicht editierbar).
- In die Spalte *Name* tragen Sie die Person ein.
- Die Liste wird in Vorschau und Export übernommen.

Über das Augen-Symbol blenden Sie die Namensliste ein/aus, über das Zahnrad-Symbol öffnen Sie die Formatierung (Schrift/Farben/Größe). Dort legen Sie auch die **Zahl der Spalten** (1–4) fest, in die die Namensliste in der Bildvorschau und im Export (JPG/PDF) aufgeteilt wird. Die Eingabetabelle im Text-/Listenbereich bleibt davon unverändert und zeigt weiterhin nur die zwei Spalten *Nr.* und *Name*.

![Namensliste im Text-/Listenbereich](Pictures/07_namensliste.png)
<!-- Zeigt: rechte Spalte mit ausgefüllter Namensliste, Augen- und Zahnrad-Icon sichtbar -->

---

## 8. Überschrift, Beschreibung, Bild-ID

Rechts stehen eigene Felder für:

- **Überschrift**
- **Beschreibung** (Bildinformation)
- **Bild-ID** (z. B. Archivsignatur)

Jeder Block kann separat über das Augen-Symbol ein- oder ausgeblendet und über das Zahnrad-Symbol formatiert werden. Wenn Sie alle Elemente einschließlich der Namensliste ausblenden, erhalten Sie ein reines Bild mit Nummern, das sich z. B. für Präsentationen oder den Druck eignet.

![Überschrift, Beschreibung, Bild-ID](Pictures/08_ueberschrift_info_id.png)
<!-- Zeigt: rechte Spalte mit ausgefüllten Feldern für Überschrift/Beschreibung/Bild-ID -->

---

## 9. Speichern & Export

### 9.1 Als JPG speichern

- Speichert das Ergebnisbild mit Nummern und Textblöcken als JPG-Datei.
- Geeignet für Weitergabe, Druck und Einbindung in Dokumente.
- Die Original-Bilddatei wird unter keinen Umständen verändert oder überschrieben.

### 9.2 Als PDF speichern

- Erzeugt eine reguläre PDF-Datei, die sich mit gängigen PDF-Programmen öffnen lässt.
- Die PDF enthält zusätzlich unsichtbar eingebettete Bearbeitungsdaten, sodass sie sich später in AutoNumber erneut öffnen und weiterbearbeiten lässt (siehe [9.4](#94-wieder-öffnen-zur-bearbeitung)).

![Beispiel für eine gespeicherte PDF-Datei](Pictures/09b_pdf_beispiel.png)
<!-- Zeigt: exportierte PDF-Datei in einem PDF-Betrachter geöffnet -->

### 9.3 Dateiname beim Speichern

In den Einstellungen (Tab **Export**) können Sie ein Suffix festlegen, das AutoNumber automatisch an den Dateinamen anhängt (Standard: `_num`), damit Original und bearbeitete Version klar getrennt bleiben. Ein bereits vorhandenes Original wird dabei nie überschrieben.

### 9.4 Wieder öffnen zur Bearbeitung

- Bereits bearbeitete Dateien (JPG, PDF) können jederzeit erneut über **Bild öffnen** geladen und weiterbearbeitet werden.
- Layout, Sichtbarkeit der Elemente, Schriftgrößen und Namenszuordnung werden dabei wiederhergestellt.

---

## 10. Einstellungen

Über das Zahnrad oben rechts in der Titelleiste öffnen Sie die **Einstellungen**. Der Dialog ist in vier Tabs gegliedert. Die hier festgelegten Werte sind Standardwerte für neue Bilder und überschreiben nie die Werte eines bereits bearbeiteten (gespeicherten) Bildes.

Für die meisten Nutzer reichen die mitgelieferten Standardwerte aus – ein Blick in die Einstellungen lohnt sich vor allem, wenn Sie viele ähnliche Fotos in Serie bearbeiten und einmalig eigene Standardgrößen/-farben festlegen möchten.

### 10.1 Formatierung

AutoNumber berechnet beim Laden eines Bildes automatisch eine Basis-Schriftgröße aus Auflösung und Größe des Bildes. Die Schieberegler in diesem Tab setzen jeweils einen Faktor relativ zu dieser Basis (100 % = Basisgröße).

Für jeden der folgenden Blöcke lassen sich Schriftgröße, Schriftfarbe und Hintergrundfarbe festlegen; bei den Nummern zusätzlich die Randfarbe:

- Nummern (inkl. Randfarbe)
- Titel
- Beschreibung
- Bild-ID
- Namensliste

Der Button **„Auf das aktuelle Bild anwenden"** überträgt alle hier eingestellten Standardwerte sofort auf das gerade geöffnete Bild.

![Einstellungen – Tab Formatierung](Pictures/10a_einstellungen_formatierung.png)
<!-- Zeigt: Formatierung-Tab mit den Schriftgrößen-/Farbreglern für Nummern, Titel, Beschreibung, Bild-ID, Namensliste -->

### 10.2 Sichtbarkeit

Legt fest, welche Elemente bei neu geöffneten Bildern standardmäßig sichtbar sind:

- Titel anzeigen
- Beschreibung anzeigen
- Bild-ID anzeigen
- Namensliste anzeigen

Diese Einstellungen wirken sich nur auf neue Bilder aus; die Sichtbarkeit lässt sich pro Bild jederzeit über die Augen-Symbole im Text-/Listenbereich individuell anpassen. Der Button **„Anwenden"** überträgt die gewählten Standardwerte sofort auf das gerade geöffnete Bild.

![Einstellungen – Tab Sichtbarkeit](Pictures/10b_einstellungen_sichtbarkeit.png)
<!-- Zeigt: Sichtbarkeit-Tab mit den vier Toggle-Switches -->

### 10.3 Erkennung

Dieser Tab steuert die automatische Gesichtserkennung für neu geöffnete Bilder:

- **Gesichtserkennung verwenden:** schaltet die automatische Erkennung beim Öffnen ein/aus. Ist sie deaktiviert, lässt sich auch die Reihenerkennung nicht aktivieren.
- **Reihenerkennung verwenden:** versucht zusätzlich, Reihen automatisch aus den erkannten Gesichtern abzuleiten.

**Position des Labels im Gesicht:** Ein 3x3-Raster legt fest, an welcher Stelle des erkannten Gesichts das Nummern-Label zentriert wird (z. B. oben links, Mitte, unten rechts) – der Standard ist „Unten Mitte" (leicht unterhalb des Kinns). Die Einstellung wirkt sich nur auf neu erkannte Gesichter aus, bereits gesetzte Label werden nicht verschoben. Über den Button **„Neu Erkennen"** direkt daneben können Sie die Erkennung sofort mit der neuen Position wiederholen.

Die Gesichtserkennung läuft vollständig lokal auf Ihrem Rechner: Es werden keine Bilder oder Daten über das Internet verschickt, und es ist keine Internetverbindung erforderlich. AutoNumber nutzt dafür ein bewährtes Verfahren aus der Bildverarbeitungsbibliothek OpenCV, das speziell auf die Erkennung von Gesichtern spezialisiert ist. Es erkennt Gesichter auch auf alten oder gescannten Fotos zuverlässig – bei seitlicher Kopfhaltung, kleinen oder unscharfen Gesichtern und ungleichmäßiger Beleuchtung. Die Erkennungsschwelle ist fest auf einen Wert eingestellt, der für die allermeisten Fotos gute Ergebnisse liefert, und daher nicht über die Einstellungen veränderbar.

Für Nutzer mit technischem Interesse: eine ausführliche Erklärung des Verfahrens findet sich im offiziellen [OpenCV-Beitrag zu YuNet](https://docs.opencv.org/4.x/df/d20/classcv_1_1FaceDetectorYN.html).

![Einstellungen – Tab Erkennung](Pictures/10c_einstellungen_erkennung.png)
<!-- Zeigt: Erkennung-Tab mit Gesichts-/Reihenerkennung-Toggles und 3x3-Anker-Raster -->

### 10.4 Export

- **Suffix für gespeicherte Dateien:** wird beim Speichern automatisch an den Dateinamen angehängt (Standard: `_num`), z. B. auch `revision_01` oder leer für kein Suffix.
- **CSV (Excel) Daten** / **JSON Daten:** aktiviert den zusätzlichen Metadaten-Export beim Speichern (siehe [Kapitel 11](#11-csvjson-metadaten-export)).
- **Jetzt exportieren:** erzeugt die CSV-/JSON-Dateien sofort für das aktuell geöffnete Bild, ohne dass dafür neu gespeichert werden muss.

![Einstellungen – Tab Export](Pictures/10d_einstellungen_export.png)
<!-- Zeigt: Export-Tab mit Suffix-Feld, CSV-/JSON-Toggles und "Jetzt exportieren"-Button -->

---

## 11. CSV/JSON-Metadaten-Export

Beim Speichern können Sie zusätzlich zur Bild-/PDF-Datei optionale Metadaten-Dateien erzeugen (Einstellungen → Tab **Export**). Die Dateien erhalten denselben Namen wie die Bilddatei, jedoch mit der Endung `.csv` bzw. `.json`. Ein Klick auf **Jetzt exportieren** erzeugt die Dateien sofort für das aktuell geöffnete Bild.

Diese Metadaten enthalten:
- Titel
- Beschreibung
- Bild-ID
- Namensliste (Nr. / Name)

**Anwendungsfall (Weiterverarbeitung):**
Die Metadaten können in **Excel**, **Datenbanken** oder anderen Anwendungen weiterverarbeitet werden.

Beispiel:
- Sie suchen in einer Datenbank oder Excel-Tabelle nach einem Namen.
- Als Ergebnis erhalten Sie die **Bild-ID** des Fotos, auf dem die Person vorkommt.
- Über die zugehörige **Nummer** im Bild können Sie die Person im Foto eindeutig zuordnen.

![CSV-Metadaten in Excel](Pictures/11a_csv_export.png)
<!-- Zeigt: exportierte CSV-Datei in Excel geöffnet -->

![JSON-Metadaten](Pictures/11b_json_export.png)
<!-- Zeigt: exportierte JSON-Datei in einem Text-/Code-Editor -->

Hinweis:
- Für reine Ansicht, Druck oder einfache Weitergabe können Sie auch ohne Metadaten-Export speichern.
- Für Auswertung, Archivierung und strukturierte Suche ist der Export **mit** Metadaten empfehlenswert.

---

## 12. Tipps für die Ahnenforschung

- Arbeiten Sie das Foto zuerst komplett auf, bevor Sie Namen eintragen: zunächst die **Ausrichtung** (Drehen), danach bei Bedarf den **Reihenmodus** einrichten und die Positionen der Nummern-Label feinjustieren. Reihenmodus und Label-Positionen lassen sich zwar auch später noch ändern, aber es ist effizienter, das vor der Namenszuordnung zu erledigen.
- Nutzen Sie die **Bild-ID** für Signaturen (Archiv, Albumseite, Quelle).
- Verwenden Sie kurze, klare Namensformen (z. B. „Anna Müller, geb. 1904“).
- Speichern Sie Zwischenschritte als bearbeitbare Datei (PDF), wenn noch Unsicherheiten bei Personen bestehen.
- Halten Sie den Mauszeiger über eine Nummer, um den zugeordneten Namen als Tooltip zu sehen – so prüfen Sie die Zuordnung schnell, gerade bei Fotos mit vielen Personen.

---

## 13. Häufige Fragen (FAQ)

**Gesichter wurden nicht erkannt – was tun?**
- Prüfen Sie Ausrichtung und Bildqualität.
- Nutzen Sie „Gesichter neu erkennen“.
- Wenn das Ergebnis weiter unpassend ist: „Alle löschen“ und Nummern manuell setzen.

**Die Nummerierungsreihenfolge passt nicht zu den Reihen im Foto?**
- Nutzen Sie den Reihenmodus (siehe [6.3](#63-reihenmodus)), um Reihengrenzen direkt im Bild festzulegen.

**Namensliste wirkt zu klein/groß?**
- Öffnen Sie den Formatierungsdialog der Namensliste (Zahnrad-Symbol) und passen Sie die Größe an.

**Warum kommt eine Warnung vor dem Drehen/Löschen/Neu erkennen?**
- Damit bereits eingetragene Namen nicht versehentlich verloren gehen.

---

## 14. Installation

AutoNumber steht auf der [GitHub-Releases-Seite](https://github.com/luni64/AutoNum/releases) zum Download bereit. Dort finden Sie zu jeder Version zwei Download-Möglichkeiten:

1. **ZIP-Archiv (portabel):** Laden Sie die ZIP-Datei herunter und entpacken Sie sie in einen beliebigen Ordner auf Ihrem PC. Starten Sie AutoNumber anschließend durch Doppelklick auf `AutoNum.exe` – es ist keine Installation nötig.
2. **Installer:** Laden Sie die Setup-Datei (`.exe`) herunter und führen Sie sie aus. Der Installer richtet AutoNumber wie eine gewöhnliche Windows-Anwendung ein, inklusive Startmenü-Eintrag.

Beide Varianten enthalten dieselbe Programmversion – welche Sie wählen, ist reine Geschmackssache. Das ZIP-Archiv eignet sich z. B. für den Einsatz von einem USB-Stick, ohne Spuren auf dem PC zu hinterlassen; der Installer ist für die dauerhafte Nutzung auf einem PC bequemer.

---

## 15. Anhang

### 15.1 Tastatur- und Maus-Kurzreferenz

| Aktion | Bedienung |
|---|---|
| Bild verschieben | Linke Maustaste ziehen |
| Zoomen | Mausrad |
| Neues Nummern-Label | Rechtsklick auf freie Bildstelle |
| Nummern-Label löschen | Rechtsklick auf bestehendes Label |
| Alle Label gemeinsam verschieben | Strg + Label ziehen |
| Reihengrenze verschieben | Ziehen der Linie |
| Reihengrenze kippen | Ziehen an einem Endpunkt |

### 15.2 Versionshinweis

Dieses Handbuch beschreibt AutoNumber V2.3. Es wird bei Bedarf um weitere Screenshots und Beispiele ergänzt.
