param([string]$BaselineZip='', [string]$ReferenceVersion='2.1.0')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $root
$relativeWork='.build\installation-validation-'+[Guid]::NewGuid().ToString('N')
$work=Join-Path $root $relativeWork
$target=Join-Path $work 'app'
$userData=Join-Path $work 'user-data'
$report=Join-Path $work 'result.json'
New-Item -ItemType Directory -Path $work -Force | Out-Null
$oldEnv=$env:KINETIX_DATA_DIR;$env:KINETIX_DATA_DIR=$userData
$driver=Join-Path $env:WINDIR 'System32\drivers\ViGEmBus.sys'
$driverBefore=if(Test-Path -LiteralPath $driver){(Get-FileHash -LiteralPath $driver -Algorithm SHA256).Hash}else{''}
$checks=[Collections.Generic.List[string]]::new()
function Assert([bool]$condition,[string]$message){if(-not $condition){throw $message};$checks.Add($message);Write-Output ('PASS: '+$message)}
function RunSetup([string]$path,[string]$log){$arguments='/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS="desktopicon" /DIR="'+$target+'" /LOG="'+$log+'"';$p=Start-Process -FilePath $path -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru;if($null -ne $p.ExitCode -and $p.ExitCode -ne 0){throw ('Setup falhou: '+$p.ExitCode+'; '+$log)}}
try{
    $oldPayload=Join-Path $work 'before-payload'
    $baselineVersion='2.2.1';$baselineSource='current-payload-smoke'
    if($BaselineZip){Expand-Archive -LiteralPath (Join-Path $root $BaselineZip) -DestinationPath $oldPayload -Force;$baselineVersion=$ReferenceVersion;$baselineSource=$ReferenceVersion+'-portable'}else{$oldPayload=Join-Path $root '.build\payload'}
    & "$PSScriptRoot\build-installer.ps1" -PayloadDir $oldPayload -TestMode -AppVersion $baselineVersion -OutputDir $relativeWork -OutputName 'Before-Setup'
    & "$PSScriptRoot\build-installer.ps1" -PayloadDir (Join-Path $root '.build\payload') -TestMode -OutputDir $relativeWork -OutputName 'After-Setup'
    RunSetup (Join-Path $work 'Before-Setup.exe') (Join-Path $work 'install-before.log')
    $beforeInstalledBytes=(Get-ChildItem -LiteralPath $target -File -Recurse|Measure-Object Length -Sum).Sum
    Assert (Test-Path -LiteralPath (Join-Path $target 'KinetixStudio.exe')) 'instalação inicial criou o executável'
    $shortcut=Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'Kinetix Studio Validation.lnk'
    Assert (Test-Path -LiteralPath $shortcut) 'atalho de área de trabalho foi criado'
    & (Join-Path $root '.build\tests\KinetixStudio.Tests.exe') --fixture (Join-Path $target 'data')
    if($LASTEXITCODE -ne 0){throw 'Fixture inválida.'}
    $selected=Get-Content -LiteralPath (Join-Path $target 'data\selected.txt') -Raw
    $original=Join-Path $target ('data\profiles\'+$selected+'.json')
    $originalHash=(Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash
    RunSetup (Join-Path $work 'After-Setup.exe') (Join-Path $work 'upgrade.log')
    $diagnostic=Join-Path $work 'diagnostics.json'
    Start-Process -FilePath (Join-Path $target 'KinetixStudio.exe') -ArgumentList ('--diagnostics "'+$diagnostic+'"') -WindowStyle Hidden -Wait
    Assert (Test-Path -LiteralPath $diagnostic) 'aplicativo instalado iniciou e gerou diagnóstico'
    $status=Get-Content -LiteralPath $diagnostic -Raw | ConvertFrom-Json
    Assert ($status.SelectedProfileId -eq $selected) 'atualização preservou o perfil selecionado'
    Assert ($status.DataDirectory -eq $userData) 'aplicativo usou a pasta de dados isolada equivalente a AppData'
    $imported=Join-Path $userData ('profiles\'+$selected+'.json')
    Assert ((Get-FileHash -LiteralPath $imported -Algorithm SHA256).Hash -eq $originalHash) 'migração preservou o conteúdo exato do perfil'
    Assert ((Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash -eq $originalHash) 'perfil portátil original não foi apagado ou alterado'
    $preferences=Join-Path $userData 'desktop.json';[IO.File]::WriteAllText($preferences,'{"MinimizeToTray":false}')
    $preferencesHash=(Get-FileHash -LiteralPath $preferences -Algorithm SHA256).Hash
    RunSetup (Join-Path $work 'After-Setup.exe') (Join-Path $work 'reinstall.log')
    Assert ((Get-FileHash -LiteralPath $preferences -Algorithm SHA256).Hash -eq $preferencesHash) 'reinstalação preservou preferências pessoais'
    Assert (Test-Path -LiteralPath $shortcut) 'atualização preservou o atalho'
    $uninstall=Join-Path $target 'unins000.exe'
    Start-Process -FilePath $uninstall -ArgumentList ('/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG="'+(Join-Path $work 'uninstall.log')+'"') -WindowStyle Hidden -Wait
    Assert (-not(Test-Path -LiteralPath (Join-Path $target 'KinetixStudio.exe'))) 'desinstalação removeu o programa'
    Assert (-not(Test-Path -LiteralPath (Join-Path $target 'ViGEmClient.dll'))) 'desinstalação removeu somente a cópia privada da biblioteca'
    Assert (Test-Path -LiteralPath $imported) 'desinstalação preservou perfis em AppData'
    Assert ((Get-FileHash -LiteralPath $preferences -Algorithm SHA256).Hash -eq $preferencesHash) 'desinstalação preservou preferências'
    Assert (-not(Test-Path -LiteralPath $shortcut)) 'desinstalação removeu o atalho de validação'
    $key='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{895569E9-CC5D-4597-A223-08F2D080CD27}_is1'
    Assert (-not(Test-Path -LiteralPath $key)) 'registro de desinstalação de validação foi removido'
    & "$PSScriptRoot\build-installer.ps1" -PayloadDir (Join-Path $root '.build\payload') -TestMode -ForceMissingNet -OutputDir $relativeWork -OutputName 'Missing-Net-Setup'
    $missing=Start-Process -FilePath (Join-Path $work 'Missing-Net-Setup.exe') -ArgumentList ('/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR="'+$target+'" /LOG="'+(Join-Path $work 'missing-net.log')+'"') -WindowStyle Hidden -Wait -PassThru
    Assert (-not(Test-Path -LiteralPath (Join-Path $target 'KinetixStudio.exe')) -and (Get-Content -LiteralPath (Join-Path $work 'missing-net.log') -Raw).Contains('Required .NET Framework')) 'ausência simulada de .NET impediu instalação sem baixar runtime'
    & "$PSScriptRoot\build-installer.ps1" -PayloadDir (Join-Path $root '.build\payload') -TestMode -ForceDriverMissing -OutputDir $relativeWork -OutputName 'Missing-Driver-Setup'
    RunSetup (Join-Path $work 'Missing-Driver-Setup.exe') (Join-Path $work 'missing-driver.log')
    $finalInstalledBytes=(Get-ChildItem -LiteralPath $target -File -Recurse|Where-Object {$_.FullName -notlike ($target+'\data\*')}|Measure-Object Length -Sum).Sum
    Assert ((Get-Content -LiteralPath (Join-Path $work 'missing-driver.log') -Raw).Contains('never installed silently')) 'driver ausente foi detectado sem instalação silenciosa'
    Start-Process -FilePath (Join-Path $target 'unins000.exe') -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -WindowStyle Hidden -Wait
    $driverAfter=if(Test-Path -LiteralPath $driver){(Get-FileHash -LiteralPath $driver -Algorithm SHA256).Hash}else{''}
    Assert ($driverAfter -eq $driverBefore) 'driver compartilhado permaneceu idêntico'
    [pscustomobject]@{Passed=$checks.Count;Checks=$checks;BaselineSource=$baselineSource;BeforeInstalledBytes=$beforeInstalledBytes;FinalInstalledBytes=$finalInstalledBytes;BaselineInstallerBytes=(Get-Item -LiteralPath (Join-Path $work 'Before-Setup.exe')).Length;FinalInstallerBytes=(Get-Item -LiteralPath (Join-Path $work 'After-Setup.exe')).Length;InstalledDriverStatus=$status.Driver;DataPreserved=$true} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $report
    Set-Content -LiteralPath (Join-Path $root '.build\installer-result-path.txt') -Value $report
    Write-Output ('Relatório: '+$report)
}finally{
    if(Test-Path -LiteralPath (Join-Path $target 'unins000.exe')){Start-Process -FilePath (Join-Path $target 'unins000.exe') -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -WindowStyle Hidden -Wait}
    $env:KINETIX_DATA_DIR=$oldEnv
}
