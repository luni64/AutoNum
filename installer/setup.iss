; AutoNum Inno Setup Script

; Application metadata
#define MyAppName      "AutoNum"
#define MyAppPublisher "luni64"
#define MyAppURL       "https://github.com/luni64/AutoNum"
#define MyAppExeName   "AutoNumber.exe"

; Source paths (relative to this script — one level up from installer folder)
#define SourceDir  "..\AutoNum\bin\Release\net8.0-windows"
#define IconFile   "..\AutoNum\autonumber_taskbar.ico"
#define InstallerDir "."

; Version — update manually before each release
#define MyAppVersion "2.1.0"

; Generate WHATS_NEW.txt from template with version substituted at compile time
#define WhatsNewTemplate SourcePath + "\WHATS_NEW.template"
#define WhatsNewOutput   SourcePath + "\WHATS_NEW.txt"
#expr Exec("powershell", "-NoProfile -Command ""(Get-Content '" + WhatsNewTemplate + "' -Raw) -replace '\$\{VERSION\}', '" + MyAppVersion + "' | Set-Content -NoNewline '" + WhatsNewOutput + "'""", SourcePath, 1, SW_HIDE)

; CodeDependencies helper (provides Dependency_AddDotNet80Desktop)
; Download from: https://github.com/DomGries/InnoDependencyInstaller
#include "CodeDependencies.iss"

[Setup]
AppId={{6F3A2B1C-9D4E-4F7A-B1C2-3D4E5F6A7B8C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes

; Show "What's New" page after installation completes
InfoAfterFile={#InstallerDir}\WHATS_NEW.txt

OutputDir=bin
OutputBaseFilename=AutoNum-{#MyAppVersion}-Setup
SetupIconFile={#IconFile}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableProgramGroupPage=yes
; Required so the dependency installer can elevate if needed
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german";  MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main executable
Source: "{#SourceDir}\{#MyAppExeName}";  DestDir: "{app}"; Flags: ignoreversion

; All DLLs (including subdirectories such as runtimes\, de\)
Source: "{#SourceDir}\*.dll";            DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Runtime configuration files
Source: "{#SourceDir}\*.json";           DestDir: "{app}"; Flags: ignoreversion

; Haar cascade classifier required for face detection
Source: "{#SourceDir}\Classifiers\*";   DestDir: "{app}\Classifiers"; Flags: ignoreversion recursesubdirs createallsubdirs

; License documentation
Source: "..\THIRD_PARTY_LICENCES.md";      DestDir: "{app}\licenses"; Flags: ignoreversion
Source: "..\LICENSE.txt";                  DestDir: "{app}\licenses"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}";                            Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}";      Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";                      Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup: Boolean;
begin
  // Ensure .NET 8 Desktop Runtime (x64) is present; downloads it if missing.
  Dependency_AddDotNet80Desktop;
  Result := True;
end;
