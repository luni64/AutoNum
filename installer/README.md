# AutoNum Installer

## Prerequisites

| Tool | Notes |
|------|-------|
| [Inno Setup 6](https://jrsoftware.org/isinfo.php) | Must be installed to compile `setup.iss` |
| [InnoDependencyInstaller](https://github.com/DomGries/InnoDependencyInstaller) | Place `CodeDependencies.iss` in this folder (see below) |

### CodeDependencies.iss

Download the latest `CodeDependencies.iss` from the
[InnoDependencyInstaller releases](https://github.com/DomGries/InnoDependencyInstaller/releases)
and copy it into this `installer\` folder alongside `setup.iss`.
It provides the `Dependency_AddDotNet80Desktop` helper used in `InitializeSetup`.

## Build the installer

1. Publish the AutoNum project in **Release** configuration:

   ```
   dotnet publish AutoNum\AutoNumber.csproj -c Release
   ```

   The project pins `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`, so the
   output contains only win-x64 native libraries (a RID-less build would also
   copy natives for win-x86/arm64, linux and osx — roughly double the size).
   The release ZIP is created from the same folder the installer uses:
   `AutoNum\bin\Release\net8.0-windows\win-x64\publish`.

2. Open `installer\setup.iss` in the Inno Setup IDE (or compile from the command line):

   ```
   ISCC.exe installer\setup.iss
   ```

3. The resulting installer is written to `installer\bin\AutoNum-<version>-Setup.exe`.

## Code signing

`setup.iss` is configured to sign both setup EXE and uninstaller using an Inno Setup **Sign Tool profile**:

```pascal
SignTool=signtool $f
SignedUninstaller=yes
```

Requirements:

1. In Inno Setup IDE, open **Tools -> Configure Sign Tools...**
2. Add a sign tool named **`signtool`** (exact name) with your command, e.g.:

```
signtool.exe sign /a /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com $f
```

3. Ensure your code-signing certificate is available (certificate store or `.pfx` in your command).

## Version

Before building the installer, update the version number at the top of `setup.iss`:

```pascal
#define MyAppVersion "1.2.3"
```
