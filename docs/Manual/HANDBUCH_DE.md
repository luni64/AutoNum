# AutoNum – Benutzerhandbuch (Entwurf)

> Zielgruppe: Hobby-Genealog:innen, die Personen auf historischen Fotos nummerieren und mit Namenslisten dokumentieren möchten.

---

## 1. Was ist AutoNum?

AutoNum hilft Ihnen dabei,

- Gesichter auf Fotos automatisch zu erkennen,
- Nummern direkt auf dem Bild zu platzieren,
- eine passende Namensliste zu pflegen,
- und das Ergebnis als **JPG** oder **bearbeitbares PDF** zu speichern.

So können Sie Familienfotos, Klassenbilder oder Gruppenaufnahmen schnell für Ihre Ahnenforschung aufbereiten.

---

## 2. Überblick über die Oberfläche

### 2.1 Gesamtansicht

Die Oberfläche besteht aus drei Bereichen:

1. **Links: Datei-Bereich**
   - Bild öffnen
   - Als JPG speichern
   - Als PDF speichern
   - Kurzhinweise zur Bedienung

2. **Mitte: Bildvorschau**
   - Hier sehen Sie das Foto mit Nummern und Zusatztexten.
   - Unten befinden sich Aktionsbuttons (z. B. Drehen, Zoom, Neu-Erkennung).

3. **Rechts: Text- und Listenbereich**
   - Überschrift
   - Beschreibung/Bildinformation
   - Bild-ID
   - Namensliste (Nr. / Name)

**Vorhandener Screenshot:**

![AutoNum Hauptfenster](Pictures/UI.jpg)

**Platzhalter für weitere Detailbilder:**
- [BILD-PLATZHALTER: Hauptfenster mit markierten Bereichen]
- [BILD-PLATZHALTER: Buttons unter der Bildvorschau]
- [BILD-PLATZHALTER: Formatierungsdialog]

---

## 3. Schnellstart (typischer Ablauf)

1. **Bild öffnen** (linke Spalte).
2. Bei Bedarf das Bild mit **Drehen** (Button unter der Vorschau) korrekt ausrichten.
3. Gesichter werden automatisch erkannt und nummeriert.
4. In der **Namensliste** rechts die Namen zu den Nummern eintragen.
5. Optional Überschrift, Beschreibung und Bild-ID ergänzen.
6. Das Ergebnis als **JPG** oder **PDF** speichern.

---

## 4. Detaillierte Bedienung

### 4.1 Bild öffnen

- Klicken Sie auf **Bild öffnen**.
- Unterstützte Formate sind im Datei-Bereich angegeben (z. B. JPG, PNG, TIFF, BMP, GIF; ggf. PDF je nach Workflow).
- Nach dem Öffnen startet die Gesichtserkennung automatisch.

---

### 4.2 Navigation in der Bildvorschau

- **Mausrad:** Ein-/Auszoomen
- **Linke Maustaste (ziehen):** Bild verschieben
- **Zoom auf Inhalt anpassen:** Button mit „Seite einpassen“-Symbol

Tipp: Wenn die Ansicht „verrutscht“ ist, hilft meist ein Klick auf **Zoom auf Inhalt anpassen**.

---

### 4.3 Bild drehen (90°)

- Nutzen Sie den **Drehen-Button** unter der Bildvorschau.
- Jeder Klick dreht das Bild um **90° im Uhrzeigersinn**.

Wichtig:
- Wenn bereits Namen in der Namensliste eingetragen sind, erscheint vorher eine Sicherheitsabfrage.
- Nach dem Drehen wird das Bild neu ausgewertet (Gesichtserkennung läuft erneut).

---

### 4.4 Nummerierung bearbeiten

Unter der Vorschau finden Sie die Gruppe **„Nummerierung“**:

- **Neu nummerieren**
  - Vergibt die Nummern anhand eines rudimentären Reihen-Algorithmus neu.
  - Da das Erkennen von Reihen in alten Gruppenfotos oft schwierig und nicht eindeutig ist, kann die Reihenfolge manchmal etwas durcheinander wirken.
  - **Tipp:** Falls Sie mit der Reihenfolge nicht zufrieden sind, können Sie die Nummern einer Reihe manuell möglichst horizontal anordnen. Klicken Sie dann auf **„Neu nummerieren“** und ziehen Sie anschließend die Nummern an die gewünschten Stellen im Bild.
  - Neu nummerieren erhält die Zuordnung der Namens-Label zu den Feldern. Sie müssen also nicht erneut alle Namen eintragen.

- **Alle löschen**
  - Entfernt alle aktuell gesetzten Nummern-Label und die zugehörigen Namen.
  - Sinnvoll, wenn die automatische Erkennung ein unpassendes Ergebnis geliefert hat und Sie lieber sauber manuell starten möchten.

- **Gesichter neu erkennen**
  - Startet die automatische Erkennung erneut auf dem aktuellen Bild.
  - Sinnvoll, wenn Sie die Nummerierung gelöscht haben und einen neuen Erkennungslauf durchführen möchten.
  - Bitte beachten Sie, dass eine erneute Erkennung alle bestehenden Nummern-Labels und Namen entfernt. Die Namen müssen anschließend erneut zugeordnet werden.

- **Formatieren**
  - Öffnet die Darstellungseinstellungen für Nummern (Schrift, Farben, Größe).

**Wichtiger Hinweis:**
Wenn bereits Namen eingetragen sind, erscheint beim **Löschen** und beim **Neu-Erkennen** eine Sicherheitsabfrage, damit keine Zuordnungen versehentlich verloren gehen.

**Typischer Arbeitsweg bei schlechtem Erkennungsergebnis:**
1. „Alle löschen“
2. Nummern manuell setzen/verschieben (rechte Maustaste)
3. Namen in der Liste eintragen

#### Manuelle Nummern setzen und löschen

Wenn Sie manuell arbeiten möchten, können Sie direkt in der Bildvorschau mit der rechten Maustaste arbeiten:

- **Rechte Maustaste auf freie Stelle:** erzeugt eine neue Nummer
- **Rechte Maustaste auf bestehende Nummer:** löscht diese Nummer

So können Sie auch bei schwierigen Fotos (unscharf, seitlich, teilweise verdeckt) schnell eine saubere Nummerierung aufbauen.

---

### 4.5 Namensliste pflegen

Rechts im Bereich **Namensliste**:

- Spalte **Nr.** zeigt die Bildnummer.
- In **Name** tragen Sie die Person ein.
- Die Liste wird in Vorschau und Export übernommen.

Optional:
- Über das Zahnrad-Symbol können Sie die Darstellung (Schrift/Farben/Größe/Zahl der Spalten) anpassen.

---

### 4.6 Überschrift, Beschreibung, Bild-ID

Rechts stehen eigene Felder für:

- **Überschrift**
- **Beschreibung / Bildinformation**
- **Bild-ID** (z. B. Archivsignatur)

Jeder Block kann separat ein- oder ausgeblendet und formatiert werden. Wenn Sie alle Elemente einschließlich der Namensliste ausblenden, erhalten Sie ein reines Bild mit Nummern, das sich z. B. für Präsentationen oder den Druck eignet.

---

## 5. Speichern und Wiederöffnen

### 5.1 Als JPG speichern

- Speichert das Ergebnisbild mit Nummern und Textblöcken.
- Geeignet für Weitergabe, Druck und Einbindung in Dokumente.

### 5.2 Als PDF speichern

- Erzeugt eine PDF-Datei. Mit AutoNum erzeugte PDF-Dateien sind reguläre PDF-Dateien und können mit gängigen PDF-Programmen geöffnet werden. Sie lassen sich später in AutoNum erneut öffnen und weiterbearbeiten.

Beispiel für eine PDF-Datei: [BILD-PLATZHALTER: PDF-Vorschau]


### 5.3 Optionale Metadaten-Speicherung

Beim Speichern können Sie optional Metadaten (z. B. in CSV/JPEG-Metadaten) ablegen. Die Dateien erhalten denselben Namen wie die Bilddatei, jedoch mit der Endung `.csv` bzw. `.jpg`.

Diese Metadaten enthalten:
- Erstellungsdatum
- Titel
- Beschreibung
- Bild-ID
- Namensliste

**Anwendungsfall (Weiterverarbeitung):**
Die Metadaten können in **Excel**, **Datenbanken** oder anderen Anwendungen weiterverarbeitet werden.

Beispiel:
- Sie suchen in einer Datenbank nach einem Namen.
- Als Ergebnis erhalten Sie die **Bild-ID** des Fotos, auf dem die Person vorkommt.
- Über die zugehörige **Nummer** im Bild können Sie die Person im Foto eindeutig zuordnen.

Beispiele für CSV- und JPG-Metadaten: [BILD-PLATZHALTER: CSV-Metadaten-Vorschau] [BILD-PLATZHALTER: JPG-Metadaten-Vorschau]

Hinweis:
- Für reine Ansicht, Druck oder einfache Weitergabe können Sie auch ohne Metadaten speichern.
- Für Auswertung, Archivierung und strukturierte Suche ist Speichern **mit** Metadaten empfehlenswert.

### 5.4 Wieder öffnen zur Bearbeitung

- Bereits bearbeitete Dateien (JPG, PDF) können erneut geöffnet und weiterbearbeitet werden.
- Layout, Sichtbarkeit der Elemente und Schriftgrößen werden dabei wiederhergestellt.

---

## 6. Einstellungen

Über das Zahnrad oben rechts öffnen Sie die **Einstellungen**.

> Hinweis: Der Tab **„Erkennen“** wird hier bewusst nicht beschrieben.

### 6.1 Tab „Schriften“

Hier legen Sie Standardwerte für die Darstellung fest, die bei neuen Bildern verwendet werden.

Sie können u. a. einstellen:
- Schriftgröße als Prozentwert (z. B. 100 %, 125 %, 150 %)
- Farben und Darstellung für die relevanten Text-/Nummernblöcke
- Übernehmen von Werten als Standard

Praxis-Tipp für genealogische Serien:
Wenn Sie häufig ähnliche Gruppenfotos bearbeiten, lohnt es sich, einmal passende Standardwerte festzulegen. Das spart bei jedem weiteren Bild Zeit.

### 6.2 Tab „Speichern“

Hier steuern Sie das Speicherverhalten, insbesondere die Dateinamenvorschläge.

Typisch ist z. B. die Arbeit mit vorgeschlagenen Namenszusätzen (wie `_num`), damit Originaldatei und bearbeitete Version klar getrennt bleiben.

Tipp: Nutzen Sie konsistente Dateinamen pro Sammlung/Album, damit exportierte JPG- und PDF-Dateien später leichter wiederzufinden sind.

---

## 7. Tipps für Ahnenforschung

- Arbeiten Sie zuerst die **Ausrichtung** des Fotos ab (Drehen), dann erst Namen eintragen.
- Nutzen Sie die **Bild-ID** für Signaturen (Archiv, Albumseite, Quelle).
- Verwenden Sie kurze, klare Namensformen (z. B. „Anna Müller, geb. 1904“).
- Speichern Sie Zwischenschritte als bearbeitbare Datei, wenn noch Unsicherheiten bei Personen bestehen.

---

## 8. Häufige Fragen (FAQ)

**Gesichter wurden nicht erkannt – was tun?**
- Prüfen Sie Ausrichtung und Bildqualität.
- Nutzen Sie „Gesichter neu erkennen“.
- Wenn das Ergebnis weiter unpassend ist: „Alle löschen“ und Nummern manuell setzen.

**Namensliste wirkt zu klein/groß?**
- Öffnen Sie den Formatdialog der Namensliste und passen Sie die Größe an.

**Warum kommt eine Warnung vor dem Drehen/Löschen?**
- Damit bereits eingetragene Namen nicht versehentlich verloren gehen.

---

## 9. Platzhalter für zusätzliche Dokumentationsbilder

- [BILD-PLATZHALTER: Schritt 1 – Bild öffnen]
- [BILD-PLATZHALTER: Schritt 2 – Drehen und Zoom anpassen]
- [BILD-PLATZHALTER: Schritt 3 – Namensliste ausfüllen]
- [BILD-PLATZHALTER: Schritt 4 – Export als JPG/PDF]
- [BILD-PLATZHALTER: Einstellungen – Schriftgrößen als Prozent]

---

## 10. Versionshinweis

Dieses Handbuch ist ein **erster Entwurf** und kann mit zusätzlichen Screenshots und Beispielen weiter ausgebaut werden.
