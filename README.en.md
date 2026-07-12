# AutoNumber

*[Deutsche Version](README.md)*

**AutoNumber** helps genealogists identify people in group photos: faces are detected automatically, numbered, and linked to a name list — exportable as a JPG or an editable PDF.

![AutoNumber main window](docs/Manual/Pictures/02_oberflaeche_gesamt.png)

## 📖 User Manual

The full manual (installation, all features, tips, FAQ) is available in the **[online User Manual](https://luni64.github.io/AutoNum/MANUAL_EN/)** (source: [docs/Manual/MANUAL_EN.md](docs/Manual/MANUAL_EN.md)).

## Features

- Automatic face detection with manual fine-tuning (add, move, delete numbers)
- Row mode for row-by-row numbering of group photos
- Name list, title, description, and image ID as metadata
- Export as a flat JPG or as an editable, reopenable PDF
- Optional CSV/JSON metadata export for Excel, databases, or archive systems
- Customizable appearance (font, color, size per element)

## Installation

Ready-to-use builds are available on the [releases page](https://github.com/luni64/AutoNum/releases):

- Download the **ZIP archive**, extract it, and run `AutoNum.exe` — no installation needed, or
- Download the **setup installer** and run it.

Requires the **.NET 8.0** runtime; if automatic installation fails, it can be downloaded manually from [Microsoft's website](https://dotnet.microsoft.com/download/dotnet/8.0).

## Dependencies

| Library | Purpose | License |
|------------|-------|--------|
| **CommunityToolkit.Mvvm** | MVVM utilities and messenger | [MIT](https://licenses.nuget.org/MIT) |
| **Emgu.CV.runtime.windows / Emgu.CV.Wpf** | Face detection (OpenCV for .NET/WPF) | Dual license (GPLv3 or commercial) |
| **MahApps.Metro (+ IconPacks)** | WPF UI styling and icons | [MIT](https://licenses.nuget.org/MIT) |
| **QuestPDF** | PDF generation | Dual license (Community / Professional / Enterprise) |

See [THIRD_PARTY_LICENCES.md](THIRD_PARTY_LICENCES.md) for full details and license notices.

## Changelog

All versions and their changes are listed in the [changelog](https://luni64.github.io/AutoNum/CHANGELOG/).

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE.txt) for details.

## Contact

For support or questions, please open a ticket on the GitHub issue tracker.
