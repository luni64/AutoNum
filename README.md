# AutoNumber

*[English version](README.en.md)*

**AutoNumber** hilft Genealogen, Personen auf Gruppenfotos zu identifizieren: Gesichter werden automatisch erkannt, nummeriert und mit einer Namensliste verknüpft — exportierbar als JPG oder bearbeitbares PDF.

![AutoNumber Hauptfenster](docs/Manual/Pictures/02_oberflaeche_gesamt.png)

## 📖 Benutzerhandbuch

Die vollständige Anleitung (Installation, alle Funktionen, Tipps, FAQ) steht im **[Benutzerhandbuch (HANDBUCH_DE.md)](docs/Manual/HANDBUCH_DE.md)**.

## Funktionen

- Automatische Gesichtserkennung mit manueller Nachbearbeitung (Nummern setzen, verschieben, löschen)
- Reihenmodus für zeilenweise Nummerierung von Gruppenfotos
- Namensliste, Titel, Beschreibung und Bild-ID als Metadaten
- Export als flaches JPG oder als bearbeitbares, wieder öffenbares PDF
- Optionaler CSV-/JSON-Metadaten-Export für Excel, Datenbanken oder Archivsysteme
- Anpassbares Erscheinungsbild (Schriftart, Farbe, Größe je Element)

## Installation

Fertige Builds gibt es auf der [Releases-Seite](https://github.com/luni64/AutoNum/releases):

- **ZIP-Archiv** herunterladen, entpacken und `AutoNum.exe` starten — keine Installation nötig, oder
- **Setup-Installer** herunterladen und ausführen.

Details siehe [Kapitel 14 im Handbuch](docs/Manual/HANDBUCH_DE.md#14-installation).

Benötigt wird das **.NET 8.0**-Runtime; falls die automatische Installation fehlschlägt, steht sie auf [Microsofts Website](https://dotnet.microsoft.com/de-de/download/dotnet/8.0) zum manuellen Download bereit.

## Abhängigkeiten

| Bibliothek | Zweck | Lizenz |
|------------|-------|--------|
| **CommunityToolkit.Mvvm** | MVVM-Utilities und Messenger | [MIT](https://licenses.nuget.org/MIT) |
| **Emgu.CV.runtime.windows / Emgu.CV.Wpf** | Gesichtserkennung (OpenCV für .NET/WPF) | Dual-Lizenz (GPLv3 oder kommerziell) |
| **MahApps.Metro (+ IconPacks)** | WPF-UI-Styling und Icons | [MIT](https://licenses.nuget.org/MIT) |
| **QuestPDF** | PDF-Erzeugung | Dual-Lizenz (Community / Professional / Enterprise) |

Siehe [THIRD_PARTY_LICENCES.md](THIRD_PARTY_LICENCES.md) für vollständige Details und Lizenzhinweise.

## Änderungsprotokoll

Alle Versionen und ihre Änderungen sind im [Changelog](docs/release/CHANGELOG.md) aufgeführt.

## Lizenz

Dieses Projekt ist unter der MIT-Lizenz lizenziert. Siehe die Datei [LICENSE](LICENSE.txt) für Details.

## Kontakt

Für Support oder Fragen öffnen Sie bitte ein Ticket im GitHub-Issue-Tracker.
