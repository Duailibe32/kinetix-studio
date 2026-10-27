#ifndef AppVersion
  #define AppVersion "2.2.0"
#endif
#ifndef PayloadDir
  #define PayloadDir "..\.build\payload"
#endif
#ifndef OutputFolder
  #define OutputFolder "..\installer-output"
#endif
#ifndef OutputName
  #define OutputName "KinetixStudio-Setup"
#endif
#ifdef TestMode
  #define ShortcutName "Kinetix Studio Validation"
#else
  #define ShortcutName "Kinetix Studio"
#endif
[Setup]
#ifdef TestMode
AppId={{895569E9-CC5D-4597-A223-08F2D080CD27}
AppName=Kinetix Studio Validation
DefaultDirName={localappdata}\Programs\KinetixStudioValidation
DefaultGroupName=Kinetix Studio Validation
#else
AppId={{5764E9C7-6D74-4407-A6EF-9E9A6A5A91A2}
AppName=Kinetix Studio
DefaultDirName={localappdata}\Programs\KinetixStudio
DefaultGroupName=Kinetix Studio
#endif
AppVersion={#AppVersion}
AppPublisher=Duailibe32
AppPublisherURL=https://github.com/Duailibe32/kinetix-studio
AppSupportURL=https://github.com/Duailibe32/kinetix-studio/issues
OutputDir={#OutputFolder}
OutputBaseFilename={#OutputName}
SetupIconFile=..\assets\brand\kinetix.ico
UninstallDisplayIcon={app}\KinetixStudio.exe
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
LZMANumBlockThreads=1
DisableProgramGroupPage=yes
CloseApplications=no
RestartApplications=no
AppMutex=Local\KinetixStudio.Desktop,Local\Veylora.Desktop
SetupLogging=yes
[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; Flags: unchecked
Name: "driver"; Description: "Baixar e abrir o instalador oficial do ViGEmBus 1.22.0 (somente se ausente)"; Flags: unchecked; Check: DriverMissing
[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Kinetix Studio"; Filename: "{app}\KinetixStudio.exe"
Name: "{group}\Desinstalar Kinetix Studio"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#ShortcutName}"; Filename: "{app}\KinetixStudio.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\KinetixStudio.exe"; Description: "Abrir Kinetix Studio"; Flags: nowait postinstall skipifsilent unchecked
[Code]
var LegacyPage: TInputQueryWizardPage; DriverReady: Boolean; DriverFile: String;
function DriverMissing: Boolean;
begin
#ifdef ForceDriverMissing
  Result := True;
#else
  Result := not (RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\ViGEmBus') and FileExists(ExpandConstant('{sys}\drivers\ViGEmBus.sys')));
#endif
end;
function InitializeSetup: Boolean;
var Code: Integer;
begin
#ifdef ForceMissingNet
  Result := False;
#else
  Result := IsDotNetInstalled(net48, 0);
#endif
  if not Result then begin
    Log('Required .NET Framework 4.8 or compatible runtime not found.');
    if not WizardSilent then
      if MsgBox('O Kinetix Studio requer .NET Framework 4.8 ou uma versão compatível, como 4.8.1. Nada foi instalado. Deseja abrir a página oficial da Microsoft?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet-framework/net48', '', '', SW_SHOWNORMAL, ewNoWait, Code);
  end;
end;
procedure BrowseLegacy(Sender: TObject);
var Folder: String;
begin Folder := LegacyPage.Values[0]; if BrowseForFolder('Escolha a pasta do aplicativo anterior', Folder, False) then LegacyPage.Values[0] := Folder; end;
procedure InitializeWizard;
var BrowseButton: TNewButton;
begin
  if DriverMissing then Log('ViGEmBus missing: optional, consent required, never installed silently.') else Log('ViGEmBus present: shared driver will not be changed.');
  LegacyPage := CreateInputQueryPage(wpSelectDir, 'Perfis da versão portátil', 'Importação opcional, sem apagar os originais.', 'Se você usava uma versão portátil em outra pasta, escolha a pasta do aplicativo anterior. Deixe vazio se não precisar importar. Os dados existentes em AppData serão preservados.');
  LegacyPage.Add('Pasta do aplicativo anterior (contém a subpasta data):', False);
  LegacyPage.Values[0] := '';
  BrowseButton := TNewButton.Create(LegacyPage); BrowseButton.Parent := LegacyPage.Surface; BrowseButton.Caption := 'Procurar...'; BrowseButton.SetBounds(0, LegacyPage.Edits[0].Top + LegacyPage.Edits[0].Height + ScaleY(12), ScaleX(100), ScaleY(25)); BrowseButton.OnClick := @BrowseLegacy;
end;
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := (PageID = LegacyPage.ID) and (GetPreviousData('Installed', '') = 'yes');
end;
procedure RegisterPreviousData(PreviousDataKey: Integer);
begin SetPreviousData(PreviousDataKey, 'Installed', 'yes'); end;
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (CurPageID = LegacyPage.ID) and (LegacyPage.Values[0] <> '') and not DirExists(AddBackslash(LegacyPage.Values[0]) + 'data\profiles') then begin
    MsgBox('A pasta escolhida não contém data\profiles. Selecione a pasta correta ou deixe o campo vazio.', mbError, MB_OK); Result := False; Exit;
  end;
  if (CurPageID = wpReady) and DriverMissing and WizardIsTaskSelected('driver') and not WizardSilent and not DriverReady then begin
    if MsgBox('O ViGEmBus é compartilhado com outros aplicativos e seu projeto foi arquivado. Será baixado somente o instalador oficial 1.22.0, sem o atualizador antigo. A instalação do driver exige sua confirmação e pode pedir permissão de administrador. Deseja continuar?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) <> IDYES then begin Result := False; Exit; end;
    try
      DownloadTemporaryFile('https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe', 'ViGEmBus_1.22.0.exe', '89220a7865076b342892f98865f3499fb7c4cfd673159e89d352c360fd014c6a', nil);
      DriverFile := ExpandConstant('{tmp}\ViGEmBus_1.22.0.exe'); DriverReady := True;
    except
      MsgBox('Não foi possível obter e validar o instalador oficial. O driver não foi executado. Desmarque a opção de driver para instalar apenas o aplicativo.' + #13#10 + GetExceptionMessage, mbError, MB_OK); Result := False;
    end;
  end;
end;
procedure CurStepChanged(CurStep: TSetupStep);
var Code: Integer;
begin
  if CurStep = ssPostInstall then begin
    if (LegacyPage.Values[0] <> '') and not WizardSilent then begin
      if not Exec(ExpandConstant('{app}\KinetixStudio.exe'), '--migrate-from "' + AddBackslash(LegacyPage.Values[0]) + 'data"', '', SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then
        MsgBox('O aplicativo foi instalado, mas a importação não foi concluída. Os dados originais foram preservados. Consulte o histórico local.', mbInformation, MB_OK);
    end;
    if DriverReady and DriverMissing and not WizardSilent then
      if not ShellExec('runas', DriverFile, '', '', SW_SHOWNORMAL, ewWaitUntilTerminated, Code) then
        MsgBox('O driver não foi instalado. O Kinetix Studio permanece instalado; você pode instalar o driver oficial mais tarde.', mbInformation, MB_OK);
  end;
end;
