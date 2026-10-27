# Desenvolvimento

Use Windows x64, PowerShell 7 e .NET Framework 4.8. Não é necessário instalar o SDK .NET.

```powershell
pwsh -File .\build.ps1
.\.build\tests\KinetixStudio.Tests.exe
```

O build funciona sem rede: `scripts/restore.ps1` valida e extrai a DLL nativa do pacote oficial em `dependencies/vigem-client.zip`. Não use cópias de DLLs de outros aplicativos.

Para testar o driver já instalado, fora de uma partida:

```powershell
.\.build\tests\KinetixStudio.Tests.exe --integration
```

Os testes com `--integration` criam dispositivos temporários e os removem ao sair. A CI executa apenas testes que não precisam de driver.

`--input-integration` acrescenta uma janela temporária para verificar hooks reais por XInput; precisa de foco exclusivo e desktop interativo. Não execute durante uma partida.

Para montar o ZIP portátil e seu SHA-256:

```powershell
pwsh -File .\build.ps1 -Package
pwsh -File .\build.ps1 -Package -Installer
pwsh -File .\scripts\test-installer.ps1
```

Gerar o instalador requer Inno Setup. Na primeira vez, o script baixa a versão oficial fixa e valida hash e assinatura; o compilador fica em `.build/tools`, fora da distribuição. Os testes de instalação usam AppId próprio, diretório novo por execução e preservam os dados do teste na desinstalação. Use `-BaselineZip` com um ZIP 2.1 preservado para comparação histórica.

Arquitetura e limites estão em `docs/ARCHITECTURE.md`. Preserve F12 como desligamento de emergência, neutralize a saída na perda de foco e mantenha perfis, logs e relatórios de engenharia reversa fora dos commits. O projeto usa apenas APIs públicas do Windows e do ViGEmClient.
