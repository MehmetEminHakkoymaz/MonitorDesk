; Build with Build-Installer.ps1. The runtime lives beside the app, not system-wide.
#ifndef AppVersion
  #error AppVersion must be passed by the build script.
#endif
#ifndef PublishDir
  #error PublishDir must be passed by the build script.
#endif
#ifndef InstallerOutput
  #error InstallerOutput must be passed by the build script.
#endif

[Setup]
; Keep this ID stable so future installers upgrade the same installation.
AppId={{EB429AE2-66F9-4DD9-B845-5412B2997C12}
AppName=MonitorDesk
AppVersion={#AppVersion}
AppPublisher=Mehmet Emin Hakkoymaz
AppPublisherURL=https://github.com/MehmetEminHakkoymaz/MonitorDesk
AppSupportURL=https://github.com/MehmetEminHakkoymaz/MonitorDesk/issues
AppUpdatesURL=https://github.com/MehmetEminHakkoymaz/MonitorDesk/releases
DefaultDirName={localappdata}\Programs\MonitorDesk
DefaultGroupName=MonitorDesk
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir={#InstallerOutput}
OutputBaseFilename=MonitorDesk-Setup-{#AppVersion}-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\MonitorDesk.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autodesktop}\MonitorDesk"; Filename: "{app}\MonitorDesk.exe"; WorkingDir: "{app}"
Name: "{autoprograms}\MonitorDesk"; Filename: "{app}\MonitorDesk.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\MonitorDesk.exe"; Description: "Launch MonitorDesk"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Command: String;
begin
  if CurUninstallStep = usUninstall then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'MonitorDesk', Command) then
      if Pos(Lowercase('"' + ExpandConstant('{app}\MonitorDesk.exe') + '"'), Lowercase(Command)) = 1 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'MonitorDesk');
end;
