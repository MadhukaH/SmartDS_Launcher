; =====================================================================
; SmartDS Auto Launcher - Inno Setup 6 Script
; Production Windows Installer for SmartDS Auto Launcher
; =====================================================================

#define MyAppName "SmartDS Auto Launcher"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "SmartDS"
#define MyAppExeName "SmartDSAutoLauncher.exe"
#define MyAppURL "https://github.com/SmartDS"
#define OutputFileName "SmartDS_Auto_Launcher_Setup"

[Setup]
; Basic Application Identification
AppId={{C8E64A12-B82E-4D88-99F7-70327318C4D2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Installation Directories
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output Configuration
OutputDir=..\Output
OutputBaseFilename={#OutputFileName}
SetupIconFile=..\SmartDSAutoLauncher\Resources\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression & Architecture
Compression=lzma2/max
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

; Privileges & Modern UI
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
WizardStyle=modern
DisableWelcomePage=no

; Safe Installation Policies
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Launch SmartDS Auto Launcher automatically when logging into Windows"; GroupDescription: "Windows Integration:"

[Files]
; Published Application Files
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Application Icon
Source: "..\SmartDSAutoLauncher\Resources\app.ico"; DestDir: "{app}\Resources"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Auto-start on Windows User Login (Task selected during installation)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SmartDSAutoLauncher"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: autostart

[Run]
; Option to launch application immediately after setup finishes
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// =====================================================================
// Custom Pascal Scripting: Safe Uninstallation & Config Preservation
// =====================================================================

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ConfigPath: string;
  RegPath: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // 1. Remove Auto-Start Registry Key from HKCU Run
    RegPath := 'Software\Microsoft\Windows\CurrentVersion\Run';
    if RegValueExists(HKCU, RegPath, 'SmartDSAutoLauncher') then
    begin
      RegDeleteValue(HKCU, RegPath, 'SmartDSAutoLauncher');
    end;

    // 2. Safe Configuration Preservation:
    // User configuration is located in %LocalAppData%\SmartDSAutoLauncher\settings.json
    // By default, it is PRESERVED. The user is prompted if they wish to remove it.
    ConfigPath := ExpandConstant('{localappdata}\SmartDSAutoLauncher');
    if DirExists(ConfigPath) then
    begin
      if MsgBox('Do you want to delete your custom SmartDS Auto Launcher settings and configuration?' + #13#10 +
                'Select No to keep your settings for future use.', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      begin
        DelTree(ConfigPath, True, True, True);
      end;
    end;
    
    // NOTE: C:\SmartDS application files and databases are NEVER touched or deleted by this uninstaller.
  end;
end;

