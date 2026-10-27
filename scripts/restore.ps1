param([Parameter(Mandatory=$true)][string]$OutputDir)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$package = Join-Path $root 'dependencies\vigem-client.zip'
$expected = 'FF460031B33E0882CD1166FE2D072D0FE97578480C61ED85DCCFA6C930FDB55E'
if (-not (Test-Path -LiteralPath $package)) { throw 'Pacote versionado dependencies/vigem-client.zip não encontrado.' }
if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $expected) { throw 'O SHA-256 do pacote ViGEm não confere.' }
$cache = Join-Path $root '.build\vigem-client'
$assemblyPath = Join-Path $cache 'lib\netstandard2.0\Nefarius.ViGEm.Client.dll'
New-Item -ItemType Directory -Path $cache -Force | Out-Null
Expand-Archive -LiteralPath $package -DestinationPath $cache -Force
$assembly = [Reflection.Assembly]::LoadFile($assemblyPath)
$stream = $assembly.GetManifestResourceStream('costura64.vigemclient.dll')
if ($null -eq $stream) { throw 'Recurso nativo x64 não encontrado.' }
$nativePath = Join-Path $OutputDir 'ViGEmClient.dll'
$nativeExpected = '00303F99E1968D523FBD08A7C691D60D145B7335059EC593947841D124C10875'
if ((Test-Path -LiteralPath $nativePath) -and (Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash -eq $nativeExpected) { $stream.Dispose(); return }
try { $destination = [IO.File]::Create($nativePath); try { $stream.CopyTo($destination) } finally { $destination.Dispose() } } finally { $stream.Dispose() }
if ((Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash -ne $nativeExpected) { throw 'O SHA-256 da DLL nativa não confere.' }
