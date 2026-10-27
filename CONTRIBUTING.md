# Desenvolvimento

Use Windows x64, PowerShell 7 e .NET Framework 4.8. Não é necessário instalar o SDK .NET.

```powershell
pwsh -File .\build.ps1
.\dist\KinetixStudio.Tests.exe
```

O build funciona sem rede: `scripts/restore.ps1` valida e extrai a DLL nativa do pacote oficial em `dependencies/vigem-client.zip`. Não use cópias de DLLs de outros aplicativos.

Para testar o driver já instalado, fora de uma partida:

```powershell
.\dist\KinetixStudio.Tests.exe --integration
```

Os testes com `--integration` criam dispositivos temporários e os removem ao sair. A CI executa apenas testes que não precisam de driver.

Para montar o ZIP portátil e seu SHA-256:

```powershell
pwsh -File .\build.ps1 -Package
```

Arquitetura e limites estão em `docs/ARCHITECTURE.md`. Preserve F12 como desligamento de emergência, neutralize a saída na perda de foco e mantenha perfis, logs e relatórios de engenharia reversa fora dos commits. O projeto usa apenas APIs públicas do Windows e do ViGEmClient.
