# Release process

Step-by-step process for releasing AutoNumber version X.Y.Z, as executed for
v2.3.0. Commands are PowerShell from the repo root; `gh` is the GitHub CLI
(`D:\apps\githubcli\gh.exe`).

## 1. Version bump (two places)

- `AutoNum\AutoNumber.csproj` → `<Version>X.Y.Z</Version>` (window title reads this)
- `installer\setup.iss` → `#define MyAppVersion "X.Y.Z"`

## 2. Roll the release docs

All user-facing texts are **German**. Sources: the entries collected in
`docs/release/NEXT_RELEASE.md` during development.

1. **`docs/release/RELEASE_NOTES_DE.md`** — rewrite for this release:
   friendly prose for genealogists, sections *Neue Funktionen* /
   *Verbesserungen* / *Fehlerbehebungen*.
2. **`docs/Manual/CHANGELOG.md`** — insert a new section at the top:
   `## VX.Y.Z - YYYY-MM-DD` with **bold** labels `**Neu**` / `**Geändert**` /
   `**Behoben**` (bold, *not* headings — the manual site's sidebar should list
   only versions). Skip dev-only items that never shipped in a release.
3. **`installer\WHATS_NEW.template`** — same content, plain-text style with
   `Überschrift\n----` underlines; keep the `${VERSION}` placeholder in the
   title line (substituted at installer compile time).
4. **Reset `docs/release/NEXT_RELEASE.md`** to the empty skeleton
   (`# Next Release` / `## Features` / `## Bug Fixes`).

## 3. Build the binaries

```powershell
Remove-Item AutoNum\bin\Release -Recurse -Force        # clean slate
dotnet publish AutoNum\AutoNumber.csproj -c Release
```

Output: `AutoNum\bin\Release\net8.0-windows\win-x64\publish` (~43 files,
~100 MB). The csproj pins `RuntimeIdentifier=win-x64` and excludes QuestPDF's
LatoFont assets, so the output must contain **only** win-x64 natives and **no**
`runtimes\` or `LatoFont\` folder — if either appears, something regressed.

Smoke test: start `publish\AutoNumber.exe`, confirm it stays up, kill it.

## 4. Release ZIP (portable version)

Flat archive of the publish folder (no top-level directory), named with
underscores:

```powershell
Compress-Archive -Path AutoNum\bin\Release\net8.0-windows\win-x64\publish\* `
                 -DestinationPath AutoNumber_vX_Y_Z.zip -Force
```

Build it in a temp/scratch location — it must not be committed.

## 5. Signed installer

`setup.iss` signs via the Inno Setup sign-tool profile **`certum`** (configured
in the Inno IDE; the actual command lives in the registry). IDE builds sign
automatically; for command-line builds pass the command via `/S`. The signtool
path must be converted to its 8.3 short path, because an embedded quoted path
breaks ISCC's argument parsing:

```powershell
$st  = (Get-ItemProperty "HKCU:\Software\Jordan Russell\Inno Setup\SignTools").SignTool0
$cmd = $st.Substring($st.IndexOf('=') + 1)
if ($cmd -match '^"([^"]+)"\s+(.*)$') {
    $fso = New-Object -ComObject Scripting.FileSystemObject
    $cmd = "$($fso.GetFile($Matches[1]).ShortPath) $($Matches[2])"
}
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /Q "/Scertum=$cmd" installer\setup.iss
```

Output: `installer\bin\AutoNum-X.Y.Z-Setup.exe`. Verify the signature:

```powershell
(Get-AuthenticodeSignature installer\bin\AutoNum-X.Y.Z-Setup.exe).Status   # must be "Valid"
```

Note: if the Certum key needs a PIN, a prompt may appear during signing.

## 6. Commit, push, draft release

1. Commit the doc/version changes and push to `main` (this also redeploys the
   manual site, whose changelog then already shows the new version — accepted).
2. Create a **draft** release (tag is only created when published, so nothing
   is public yet):

```powershell
gh release create vX.Y.Z --repo luni64/AutoNum --draft --target main `
    --title "VX.Y.Z" --notes-file <body.md> `
    installer\bin\AutoNum-X.Y.Z-Setup.exe AutoNumber_vX_Y_Z.zip
```

Release body = the WHATS_NEW content plus an *Installation* section naming
both downloads (see the v2.3.0 release for the template) and the manual link
`https://autonumber.niggl-schlagbauer.de/`.

## 7. User testing, then publish

The user tests both binaries from the draft page (installer **and** portable
ZIP; always exercise face detection and PDF export — they cover the native
Emgu/QuestPDF dependencies). Fixes go to `main`; rebuild and replace assets
with `gh release upload vX.Y.Z --clobber <files>`.

On the user's go:

```powershell
gh release edit vX.Y.Z --repo luni64/AutoNum --draft=false --latest
```

This creates the tag and makes the release "Latest" — the manual site's
*Installation* nav entry (`releases/latest`) then points at it automatically.
