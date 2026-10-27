param([Parameter(Mandatory=$true)][string]$OutputDir,[Parameter(Mandatory=$true)][string]$Report)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$destination=[IO.Path]::GetFullPath((Join-Path $PWD $OutputDir))
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -LiteralPath '.\dist\ViGEmClient.dll','.\assets\brand\kinetix.ico' -Destination $destination -Force
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sources=@(Get-ChildItem -LiteralPath '.\src' -Filter '*.cs' | ForEach-Object FullName)
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /win32icon:assets\brand\kinetix.ico /main:KinetixStudio.MeasureRuntime "/out:$destination\MeasureRuntime.exe" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Runtime.Serialization.dll @sources scripts\MeasureRuntime.cs
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar benchmark.'}
$reportPath=[IO.Path]::GetFullPath((Join-Path $PWD $Report))
$process=Start-Process -FilePath (Join-Path $destination 'MeasureRuntime.exe') -ArgumentList ('"'+$reportPath+'"') -WindowStyle Hidden -PassThru
[pscustomobject]@{Pid=$process.Id;Report=$reportPath;Executable=(Join-Path $destination 'MeasureRuntime.exe')} | ConvertTo-Json
