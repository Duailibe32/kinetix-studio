param([Parameter(Mandatory=$true)][string]$OutputDir)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$package = Join-Path $root 'dependencies\vigem-client.zip'
$expected = 'FF460031B33E0882CD1166FE2D072D0FE97578480C61ED85DCCFA6C930FDB55E'
if (-not (Test-Path -LiteralPath $package)) { throw 'Pacote versionado dependencies/vigem-client.zip não encontrado.' }
if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $expected) { throw 'O SHA-256 do pacote ViGEm não confere.' }
$cache = Join-Path $root '.build\vigem-client'
New-Item -ItemType Directory -Path $cache -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $entry = $archive.GetEntry('lib/netstandard2.0/Nefarius.ViGEm.Client.dll')
    if ($null -eq $entry) { throw 'Assembly gerenciada ausente no pacote validado.' }
    $entryStream = $entry.Open()
    $buffer = [IO.MemoryStream]::new()
    try { $entryStream.CopyTo($buffer); $managedBytes = $buffer.ToArray() } finally { $entryStream.Dispose(); $buffer.Dispose() }
} finally { $archive.Dispose() }
$sha = [Security.Cryptography.SHA256]::Create()
try { $managedHash = [BitConverter]::ToString($sha.ComputeHash($managedBytes)).Replace('-','') } finally { $sha.Dispose() }
$assemblyPath = Join-Path $cache ($managedHash + '.dll')
if (-not (Test-Path -LiteralPath $assemblyPath)) { [IO.File]::WriteAllBytes($assemblyPath,$managedBytes) }
if ((Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash -ne $managedHash) { throw 'O cache da assembly foi alterado. Use outra cópia limpa do projeto.' }
$assembly = [Reflection.Assembly]::LoadFile($assemblyPath)
$stream = $assembly.GetManifestResourceStream('costura64.vigemclient.dll')
if ($null -eq $stream) { throw 'Recurso nativo x64 não encontrado.' }
$nativePath = Join-Path $OutputDir 'ViGEmClient.dll'
$nativeExpected = '00303F99E1968D523FBD08A7C691D60D145B7335059EC593947841D124C10875'
if ((Test-Path -LiteralPath $nativePath) -and (Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash -eq $nativeExpected) { $stream.Dispose(); return }
try { $destination = [IO.File]::Create($nativePath); try { $stream.CopyTo($destination) } finally { $destination.Dispose() } } finally { $stream.Dispose() }
if ((Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash -ne $nativeExpected) { throw 'O SHA-256 da DLL nativa não confere.' }
