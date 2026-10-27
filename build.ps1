param([string]$OutputDir = 'dist', [switch]$Package, [switch]$Installer)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not(Test-Path -LiteralPath $compiler)){throw 'Compilador .NET Framework não encontrado.'}
$output=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot $OutputDir))
if(-not $output.StartsWith($PSScriptRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Saída fora do projeto.'}
$toolDir=Join-Path $PSScriptRoot '.build\tools'
$testDir=Join-Path $PSScriptRoot '.build\tests'
$payload=Join-Path $PSScriptRoot '.build\payload'
New-Item -ItemType Directory -Path $output,$toolDir,$testDir,$payload -Force | Out-Null
& (Join-Path $PSScriptRoot 'scripts\restore.ps1') -OutputDir $output
$sources=@(Get-ChildItem -LiteralPath '.\src' -Filter '*.cs' | ForEach-Object FullName)
$common=@('/nologo','/platform:x64','/optimize+','/debug-','/filealign:512','/utf8output','/r:System.dll','/r:System.Core.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Runtime.Serialization.dll')
& $compiler @common /target:exe /main:KinetixStudio.AssetTool "/out:$toolDir\AssetTool.exe" @sources scripts\AssetTool.cs
if($LASTEXITCODE -ne 0){throw 'Falha no gerador de recursos.'}
& "$toolDir\AssetTool.exe" (Join-Path $PSScriptRoot 'assets\brand')
if($LASTEXITCODE -ne 0){throw 'Falha ao exportar identidade.'}
& $compiler @common /target:winexe /win32manifest:app.manifest /win32icon:assets\brand\kinetix.ico "/out:$output\KinetixStudio.exe" @sources
if($LASTEXITCODE -ne 0){throw 'Falha no build Release x64. Feche o aplicativo antes de compilar.'}
& $compiler @common /target:exe /main:KinetixStudio.Tests "/out:$testDir\KinetixStudio.Tests.exe" @sources tests\Tests.cs
if($LASTEXITCODE -ne 0){throw 'Falha nos testes.'}
Copy-Item -LiteralPath (Join-Path $output 'ViGEmClient.dll') -Destination $testDir -Force
$licenses=@('LICENSE','THIRD-PARTY-NOTICES.md','dependencies\LICENSE-ViGEmClient','dependencies\LICENSE-ViGEm.NET') | ForEach-Object {(Get-Content -LiteralPath (Join-Path $PSScriptRoot $_) -Raw)}
[IO.File]::WriteAllText((Join-Path $output 'LICENSES.txt'),($licenses -join "`r`n`r`n------------------------------`r`n"),[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath '.\installer\LEIA-ME.txt' -Destination $output -Force
$runtimeFiles=@('KinetixStudio.exe','ViGEmClient.dll','LICENSES.txt','LEIA-ME.txt')
foreach($name in $runtimeFiles){Copy-Item -LiteralPath (Join-Path $output $name) -Destination $payload -Force}
# Payload is a dedicated build folder; only explicitly allowed runtime files may be packaged.
foreach($file in Get-ChildItem -LiteralPath $payload -File){if($file.Name -notin $runtimeFiles){Remove-Item -LiteralPath $file.FullName -Force}}
if($Package){
    $archive=Join-Path $PSScriptRoot 'KinetixStudio-2.2.0-win-x64.zip'
    Compress-Archive -LiteralPath @($runtimeFiles|ForEach-Object{Join-Path $payload $_}) -DestinationPath $archive -Force
    [IO.File]::WriteAllText($archive+'.sha256',(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($archive)+[Environment]::NewLine)
}
if($Installer){& (Join-Path $PSScriptRoot 'scripts\build-installer.ps1') -PayloadDir $payload}
Write-Output "Release x64: $output\KinetixStudio.exe"
