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

1. Build the AutoNum project in **Release** configuration:

   ```
   dotnet publish AutoNum\AutoNumber.csproj -c Release
   ```
   (or build via Visual Studio — Release | Any CPU)

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
