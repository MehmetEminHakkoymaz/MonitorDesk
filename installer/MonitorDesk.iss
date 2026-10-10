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
AppName=Monilivo
AppVersion={#AppVersion}
AppPublisher=Mehmet Emin Hakkoymaz
AppPublisherURL=https://github.com/MehmetEminHakkoymaz/MonitorDesk
AppSupportURL=https://github.com/MehmetEminHakkoymaz/MonitorDesk/issues
AppUpdatesURL=https://github.com/MehmetEminHakkoymaz/MonitorDesk/releases
DefaultDirName={localappdata}\Programs\Monilivo
DefaultGroupName=Monilivo
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir={#InstallerOutput}
OutputBaseFilename=Monilivo-Setup-{#AppVersion}-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\src\MonitorDesk\Assets\MonitorDesk.ico
UninstallDisplayIcon={app}\Monilivo.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autodesktop}\Monilivo"; Filename: "{app}\Monilivo.exe"; WorkingDir: "{app}"
Name: "{autoprograms}\Monilivo"; Filename: "{app}\Monilivo.exe"; WorkingDir: "{app}"

[InstallDelete]
Type: files; Name: "{autodesktop}\MonitorDesk.lnk"
Type: files; Name: "{autoprograms}\MonitorDesk.lnk"
Type: files; Name: "{app}\MonitorDesk.exe"
Type: files; Name: "{app}\MonitorDesk.dll"
Type: files; Name: "{app}\MonitorDesk.deps.json"
Type: files; Name: "{app}\MonitorDesk.runtimeconfig.json"
Type: files; Name: "{app}\MonitorDesk.pdb"

[Run]
Filename: "{app}\Monilivo.exe"; Description: "Launch Monilivo"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  Command: String;
begin
  if CurStep = ssPostInstall then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'MonitorDesk', Command) then
      if Pos(Lowercase('"' + ExpandConstant('{app}\MonitorDesk.exe') + '"'), Lowercase(Command)) = 1 then
        RegWriteStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'MonitorDesk',
          '"' + ExpandConstant('{app}\Monilivo.exe') + '" --startup --tray');
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Command: String;
begin
  if CurUninstallStep = usUninstall then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'MonitorDesk', Command) then
      if (Pos(Lowercase('"' + ExpandConstant('{app}\Monilivo.exe') + '"'), Lowercase(Command)) = 1) or
         (Pos(Lowercase('"' + ExpandConstant('{app}\MonitorDesk.exe') + '"'), Lowercase(Command)) = 1) then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'MonitorDesk');
end;
