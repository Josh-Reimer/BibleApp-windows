; Inno Setup script for Desktop Bible.
;
; Build the payload first, then compile this:
;
;   dotnet publish BibleApp.Native.csproj -c Release -r win-x64 --self-contained true
;   iscc /DArch=x64 Installer\BibleApp.iss
;
; Pass /DArch=arm64 (and publish -r win-arm64) for ARM64 Windows.

#ifndef Arch
  #define Arch "x64"
#endif

#define AppName        "Desktop Bible"
#define AppExeName     "BibleApp.exe"
#define AppPublisher   "Josh Reimer"
#define SourceDir      "..\bin\Release\net9.0-windows\win-" + Arch + "\publish"

; The version is read out of the exe that was just published, so <Version> in
; BibleApp.Native.csproj is the single source of truth. Carrying a second copy
; here lets the installer and the assembly drift apart — and an auto-updater
; that compares the running assembly's version against the published release
; would then either never update or update forever.
;
; This requires the payload to exist, so publish before compiling.
#ifndef AppVersion
  #define FullVersion GetFileVersion(SourceDir + "\" + AppExeName)
  #if FullVersion == ""
    #error Could not read a version from the published exe. Run dotnet publish first.
  #endif
  ; GetFileVersion yields four parts ("2.0.0.0"); trim to the three-part form
  ; used by release tags and by the asset filename.
  #define AppVersion Copy(FullVersion, 1, RPos(".", FullVersion) - 1)
#endif

[Setup]
; ---------------------------------------------------------------------------
; AppId must never change. It is what makes a later install an *upgrade* rather
; than a second copy in Add/Remove Programs. Changing it orphans every existing
; installation.
; ---------------------------------------------------------------------------
AppId={{426E0E84-92F6-4F48-A42D-5E5ABDE4BFD6}

AppName={#AppName}
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
; No AppVerName — setting it makes Inno use it as the Add/Remove Programs
; DisplayName, which then reads "Desktop Bible 2.0.0" beside a DisplayVersion
; column already showing 2.0.0.
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/Josh-Reimer/BibleApp-windows

; Inno's default DisplayName is "<AppName> version <AppVersion>", which reads
; badly next to the DisplayVersion column already showing the same number.
UninstallDisplayName={#AppName}

; ---------------------------------------------------------------------------
; Per-user install. With PrivilegesRequired=lowest, {autopf} resolves to
; %LocalAppData%\Programs, so updates apply with no UAC prompt — which matters
; a great deal for an auto-updater, because a prompt on every update is a
; prompt people learn to cancel. Pass /ALLUSERS to install machine-wide.
; ---------------------------------------------------------------------------
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline
DefaultDirName={autopf}\BibleApp
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes

; Uses Restart Manager to shut a running copy down before overwriting it.
CloseApplications=yes
RestartApplications=no

; ARM64 Windows can run the x64 build under emulation, so an x64 installer is
; allowed there; an arm64 build is not allowed on plain x64.
#if Arch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif

OutputDir=..\..\dist-installer
OutputBaseFilename=BibleApp-{#AppVersion}-{#Arch}-setup
SetupIconFile=..\Assets\blackbible.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; The published output is the self-contained exe plus the 66 KJV .txt files,
; which the app reads from beside the executable.
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
; Interactive install: offer to launch, as usual.
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

; Silent install: only relaunch when explicitly asked. An auto-updater should
; invoke this installer as
;   BibleApp-setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /relaunch=yes
; so the app it just replaced comes back up. A plain silent install stays quiet.
Filename: "{app}\{#AppExeName}"; Flags: nowait; Check: WantsRelaunch

[UninstallDelete]
; Settings live outside {app} and are not removed by the file list.
Type: filesandordirs; Name: "{userappdata}\BibleApp"

[Code]
function WantsRelaunch: Boolean;
begin
  Result := CompareText(ExpandConstant('{param:relaunch|no}'), 'yes') = 0;
end;
