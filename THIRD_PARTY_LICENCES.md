# Third-Party Licenses

This project uses the following third-party dependencies (direct NuGet references from `AutoNum/AutoNumber.csproj`).

| Package | Version | License | Project |
|---|---:|---|---|
| CommunityToolkit.Mvvm | 8.4.0 | MIT | https://github.com/CommunityToolkit/dotnet |
| Emgu.CV.runtime.windows | 4.10.0.5680 | Dual license: GPLv3 (open-source use) or commercial license | https://www.emgu.com/ |
| Emgu.CV.Wpf | 4.10.0.5680 | Dual license: GPLv3 (open-source use) or commercial license | https://www.emgu.com/ |
| MahApps.Metro | 3.0.0-alpha0513 | MIT | https://github.com/MahApps/MahApps.Metro |
| MahApps.Metro.IconPacks.BoxIcons | 5.1.0 | MIT | https://github.com/MahApps/MahApps.Metro.IconPacks |
| MahApps.Metro.IconPacks.JamIcons | 5.1.0 | MIT | https://github.com/MahApps/MahApps.Metro.IconPacks |
| MahApps.Metro.IconPacks.Material | 5.1.0 | MIT | https://github.com/MahApps/MahApps.Metro.IconPacks |
| QuestPDF | 2026.6.1 | Dual license: Community / Professional / Enterprise (see vendor terms) | https://www.questpdf.com/license |

## Important Notes

- **Emgu.CV** is not BSD-only in the NuGet package used here; it is distributed under Emgu's dual-license model. If your usage does not meet GPL obligations, a commercial license may be required.
- **QuestPDF** is dual-licensed with eligibility criteria for the free Community license. Verify your use case against the official license terms.

## Full License Texts

The authoritative license texts are included in each NuGet package under your local NuGet cache, for example:

- `%USERPROFILE%\.nuget\packages\emgu.cv.runtime.windows\4.10.0.5680\LICENSE.txt`
- `%USERPROFILE%\.nuget\packages\emgu.cv.wpf\4.10.0.5680\LICENSE.txt`
- `%USERPROFILE%\.nuget\packages\questpdf\2026.6.1\LICENSE.md`

For MIT-licensed packages, see the SPDX MIT text: https://licenses.nuget.org/MIT

> This file is informational and not legal advice.