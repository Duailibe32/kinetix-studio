param([string]$OutputDir = 'dist', [switch]$Package)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Instale o .NET Framework 4.8 para compilar.' }
$output = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $OutputDir))
if (-not $output.StartsWith($PSScriptRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'A saída deve ficar dentro do projeto.' }
New-Item -ItemType Directory -Path $output -Force | Out-Null
& (Join-Path $PSScriptRoot 'scripts\restore.ps1') -OutputDir $output
$sources = @(Get-ChildItem -LiteralPath '.\src' -Filter '*.cs' | ForEach-Object FullName)
$refs = @('/r:System.dll','/r:System.Core.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Runtime.Serialization.dll')
$common = @('/nologo','/platform:x64','/optimize+','/utf8output') + $refs
& $compiler @common /target:exe /main:Veylora.AssetTool "/out:$output\AssetTool.exe" @sources scripts\AssetTool.cs
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar gerador de identidade visual.' }
& "$output\AssetTool.exe" (Join-Path $PSScriptRoot 'assets\brand')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao exportar a identidade visual.' }
Copy-Item -LiteralPath '.\assets\brand\veylora.ico' -Destination $output -Force
& $compiler @common /target:winexe /win32manifest:app.manifest /win32icon:assets\brand\veylora.ico "/out:$output\Veylora.exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilação do Veylora falhou. Feche o aplicativo antes de recompilar.' }
& $compiler @common /target:exe /main:Veylora.Tests "/out:$output\Veylora.Tests.exe" @sources tests\Tests.cs
if ($LASTEXITCODE -ne 0) { throw 'Compilação dos testes falhou.' }
Copy-Item -LiteralPath '.\README.md' -Destination (Join-Path $output 'LEIA-ME.md') -Force
Copy-Item -LiteralPath '.\THIRD-PARTY-NOTICES.md','.\LICENSE','.\CHANGELOG.md','.\dependencies\LICENSE-ViGEmClient','.\dependencies\LICENSE-ViGEm.NET' -Destination $output -Force
if ($Package) {
    $files = @('Veylora.exe','ViGEmClient.dll','veylora.ico','LEIA-ME.md','LICENSE','CHANGELOG.md','THIRD-PARTY-NOTICES.md','LICENSE-ViGEmClient','LICENSE-ViGEm.NET') | ForEach-Object {Join-Path $output $_}
    $stage = Join-Path $PSScriptRoot ('.build\portable-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage,(Join-Path $stage 'assets\brand'),(Join-Path $stage 'docs') -Force | Out-Null
    Copy-Item -LiteralPath $files -Destination $stage
    Copy-Item -LiteralPath '.\assets\brand\veylora-logo.png','.\assets\brand\veylora-logo.svg' -Destination (Join-Path $stage 'assets\brand')
    Copy-Item -LiteralPath '.\docs\veylora-studio.png','.\docs\VALIDATION.md','.\docs\ARCHITECTURE.md' -Destination (Join-Path $stage 'docs')
    Copy-Item -LiteralPath '.\CONTRIBUTING.md' -Destination $stage
    $archive = Join-Path $PSScriptRoot 'Veylora-2.0.0-win-x64.zip'
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive -Force
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText($archive+'.sha256',$hash+'  '+[IO.Path]::GetFileName($archive)+[Environment]::NewLine)
}
Write-Output "Build pronto: $output\Veylora.exe"
