param([Parameter(Mandatory=$true)][string]$BackupDirectory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$backup=[IO.Path]::GetFullPath($BackupDirectory)
if(-not(Test-Path -LiteralPath (Join-Path $backup 'workspace.zip'))){throw 'Backup completo obrigatório antes da limpeza.'}
$removed=[Collections.Generic.List[object]]::new()
function RemoveGenerated([string]$relative){
    $path=[IO.Path]::GetFullPath((Join-Path $root $relative))
    if(-not $path.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Alvo fora do projeto.'}
    if(-not(Test-Path -LiteralPath $path)){return}
    $item=Get-Item -LiteralPath $path -Force
    $items=if($item.PSIsContainer){@($item)+@(Get-ChildItem -LiteralPath $path -Force -Recurse)}else{@($item)}
    if(@($items|Where-Object {($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or ($_.PSIsContainer -and $_.Name -eq 'data')}).Count){throw 'Dados ou links encontrados; limpeza interrompida.'}
    $bytes=($items|Where-Object {-not $_.PSIsContainer}|Measure-Object Length -Sum).Sum
    $removed.Add([pscustomobject]@{Path=$relative;Bytes=$bytes})
    Remove-Item -LiteralPath $path -Recurse -Force
}
# Explicit obsolete build outputs; preserve all data and research sources.
foreach($name in @('Auralis-1.0.0-win-x64.zip','Veylora-2.0.0-win-x64.zip','Veylora-2.0.0-win-x64.zip.sha256','KinetixStudio-2.1.0-win-x64.zip','KinetixStudio-2.1.0-win-x64.zip.sha256','dashboard.png','page-1.png','page-2.png','page-3.png','page-4.png','page-5.png')){RemoveGenerated $name}
foreach($name in @('AssetTool.exe','Auralis.exe','auralis.ico','Auralis.Tests.exe','Veylora.exe','veylora.ico','Veylora.Tests.exe','KinetixStudio.Tests.exe','kinetix.ico','CHANGELOG.md','LEIA-ME.md','LICENSE','LICENSE-ViGEm.NET','LICENSE-ViGEmClient','THIRD-PARTY-NOTICES.md','VALIDACAO.md')){RemoveGenerated ('dist\'+$name)}
foreach($folder in @('artifacts\clean','artifacts\kinetix-clean','artifacts\kinetix-preview','artifacts\release')){
    if(Test-Path -LiteralPath (Join-Path $root $folder)){
        foreach($file in Get-ChildItem -LiteralPath (Join-Path $root $folder) -File){
            if($file.Extension -in @('.exe','.dll','.ico','.md','.png') -or $file.Name -like 'LICENSE*'){RemoveGenerated (Join-Path $folder $file.Name)}
        }
    }
}
foreach($folder in Get-ChildItem -LiteralPath (Join-Path $root '.build') -Directory -Filter 'portable-*'){RemoveGenerated (Join-Path '.build' $folder.Name)}
RemoveGenerated 'dependencies\vigem-client'
RemoveGenerated '.build\vigem-client\lib'
$removed|ConvertTo-Json -Depth 3|Set-Content -LiteralPath (Join-Path $root '.build\cleanup-result.json')
[pscustomobject]@{Removed=$removed.Count;BytesRecovered=($removed|Measure-Object Bytes -Sum).Sum}|ConvertTo-Json
