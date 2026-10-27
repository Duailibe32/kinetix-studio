$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$tools=Join-Path $root '.build\tools'
New-Item -ItemType Directory -Path $tools -Force | Out-Null
$setup=Join-Path $tools 'innosetup-6.7.3.exe'
$expected='9C73C3BAE7ED48D44112A0F48E66742C00090BDB5BEF71D9D3C056C66E97B732'
if(-not(Test-Path -LiteralPath $setup)){Invoke-WebRequest -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe' -OutFile $setup}
if((Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash -ne $expected){throw 'Hash do Inno Setup não confere.'}
$signature=Get-AuthenticodeSignature -LiteralPath $setup
if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Pyrsys B.V.'){throw 'Assinatura do Inno Setup não foi validada.'}
$destination=Join-Path $tools 'inno'
$arguments='/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /CURRENTUSER /NOICONS /TASKS="" /DIR="'+$destination+'"'
$p=Start-Process -FilePath $setup -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if($p.ExitCode -ne 0 -or -not(Test-Path -LiteralPath (Join-Path $destination 'ISCC.exe'))){throw 'Não foi possível preparar o compilador oficial.'}
