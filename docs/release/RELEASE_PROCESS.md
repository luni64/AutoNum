# Release process

Step-by-step process for releasing AutoNumber version X.Y.Z, as executed for
v2.3.0 and v2.4.0. Commands are PowerShell from the repo root; `gh` is the GitHub CLI
(on PATH as of v2.4.0: `C:\Program Files\GitHub CLI\gh.exe`, authenticated as `luni64`
with `repo` scope — the older `D:\apps\githubcli\gh.exe` path is gone).

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
5. **Manual version note** — chapter 15.2 ("Versionshinweis" / "Version Note")
   in **both** `docs/Manual/index.md` and `docs/Manual/MANUAL_EN.md` names the
   version the manual describes (`Dieses Handbuch beschreibt AutoNumber VX.Y`).
   Bump it to the new X.Y; it was missed in v2.4.0 and still read V2.3 after
   release. Quick check for leftovers:
   `grep -rn "V2\.[0-9]" docs/Manual/*.md | grep -v CHANGELOG`
6. **Document the feature in the manual itself**, not only in the changelog —
   new options usually need a paragraph in the relevant chapter *and* in the
   Settings chapter, in both languages, plus a fresh screenshot if a dialog
   changed. The site is the user-facing documentation; the changelog only
   says what changed, not how to use it.

The version shown top-right on the manual site is **not** stored in the repo:
MkDocs Material fetches it from `api.github.com/repos/luni64/AutoNum/releases/latest`
in the visitor's browser and caches it in `sessionStorage`. It updates by itself
once the release is published — a stale value there just means a cached session
(Ctrl+F5), not a missed edit.

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

> **Pending — sign the publish output here (see
> [CODE_SIGNING_SAC.md](CODE_SIGNING_SAC.md)).** We currently sign only the
> installer and uninstaller, so everything the app actually loads — including
> `AutoNumber.exe` and `AutoNumber.dll` — ships unsigned, and the portable ZIP
> is unsigned end to end. Windows 11's Smart App Control validates *every* code
> module and blocks hard, without a bypass. Researched 2026-07-27 and deferred
> because no user has reported a block yet; our existing Certum certificate is
> sufficient for it, no EV needed. Do this at the next release that touches the
> installer, or immediately on the first complaint. The signing pass belongs
> **here**, before the ZIP and ISCC steps, so both artifacts get it.

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
$st   = (Get-ItemProperty "HKCU:\Software\Jordan Russell\Inno Setup\SignTools").SignTool0
$args = $st.Substring($st.IndexOf('=') + 1) -replace '^"[^"]+"\s+', ''   # keep only the arguments
$fso  = New-Object -ComObject Scripting.FileSystemObject
$tool = $fso.GetFile("C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe").ShortPath
& "C:\Program Files\Inno Setup 7\ISCC.exe" /Q "/Scertum=$tool $args" installer\setup.iss
```

Two paths in the registry profile went stale and were corrected for v2.4.0 —
verify both before blaming the script:

- **Inno Setup is installed as version 7**, at `C:\Program Files\Inno Setup 7\ISCC.exe`
  (the v2.3.0 notes said "Inno Setup 6" under `Program Files (x86)`).
- The `certum` profile's `SignTool0` value still points at
  `...\Windows Kits\10\bin\10.0.26100.0\x86\signtool.exe`, **which no longer exists** —
  the Windows SDK is gone from this machine. The only `signtool.exe` left is the
  ClickOnce copy above (v4.00, 2016); it supports `/tr` and `/td`, so it signs and
  RFC3161-timestamps correctly. Hence the snippet keeps the registry *arguments*
  (thumbprint, timestamp URL) but substitutes the tool path. The certificate itself
  lives in `Cert:\CurrentUser\My` (thumbprint `7E32…F4F9`, valid to 2027-02-23) — no
  PIN prompt appeared. Reinstalling the Windows SDK and repointing the profile in the
  Inno IDE would let the original snippet work again.

The 8.3 short path is still required: an embedded quoted path breaks ISCC's `/S`
argument parsing.

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
