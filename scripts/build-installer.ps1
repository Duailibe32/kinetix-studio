param([Parameter(Mandatory=$true)][string]$PayloadDir,[string]$OutputDir='installer-output',[string]$OutputName='KinetixStudio-Setup',[string]$AppVersion='2.2.0',[switch]$TestMode,[switch]$ForceMissingNet,[switch]$ForceDriverMissing)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(-not $TestMode){
    $allowed=@('KinetixStudio.exe','ViGEmClient.dll','LICENSES.txt','LEIA-ME.txt')
    $files=@(Get-ChildItem -LiteralPath $PayloadDir -Force)
    if($files.Count -ne $allowed.Count -or @($files | Where-Object {$_.PSIsContainer -or $_.Name -notin $allowed}).Count -gt 0){throw 'Payload de produção deve conter somente os quatro arquivos necessários.'}
}
$compiler=Join-Path $root '.build\tools\inno\ISCC.exe'
if(-not(Test-Path -LiteralPath $compiler)){& (Join-Path $PSScriptRoot 'get-inno.ps1')}
$output=[IO.Path]::GetFullPath((Join-Path $root $OutputDir))
if(-not $output.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Saída do instalador fora do projeto.'}
New-Item -ItemType Directory -Path $output -Force | Out-Null
$args=@('/Qp',('/DPayloadDir='+[IO.Path]::GetFullPath($PayloadDir)),('/DOutputFolder='+$output),('/DOutputName='+$OutputName),('/DAppVersion='+$AppVersion))
if($TestMode){$args+='/DTestMode'}
if($ForceMissingNet){if(-not $TestMode){throw 'Simulação permitida apenas em TestMode.'};$args+='/DForceMissingNet'}
if($ForceDriverMissing){if(-not $TestMode){throw 'Simulação permitida apenas em TestMode.'};$args+='/DForceDriverMissing'}
& $compiler @args (Join-Path $root 'installer\KinetixStudio.iss')
if($LASTEXITCODE -ne 0){throw 'Falha no Inno Setup.'}
$setup=Join-Path $output ($OutputName+'.exe')
[IO.File]::WriteAllText($setup+'.sha256',(Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($setup)+[Environment]::NewLine)
Write-Output $setup
