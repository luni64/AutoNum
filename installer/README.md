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

## Version

Before building the installer, update the version number at the top of `setup.iss`:

```pascal
#define MyAppVersion "1.2.3"
```
