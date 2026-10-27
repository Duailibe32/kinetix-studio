# Otimização — Kinetix Studio 2.2.0

27/10/2026, Windows x64. C# / WinForms / .NET Framework 4.8 preservados. Sem migração, trimming, AOT, serviços adicionais ou componentes pagos.

## Alterações

- Release x64 com `/optimize+ /debug- /filealign:512`.
- Payload de quatro arquivos: executável, DLL nativa, licenças consolidadas e instruções. Ferramentas e testes ficam em `.build`.
- Fontes GDI+ reutilizadas; descarte do ícone e menu da bandeja. Ícone incorporado sem carregar componentes do Shell para extraí-lo.
- Atualização visual interrompida quando ocioso/oculto/minimizado; cadência do motor de entrada preservada. Menos alocações de estados neutros.
- Callbacks protegidos contra encerramento e diagnóstico do driver sem consultas simultâneas.
- Perfis em `%LOCALAPPDATA%\KinetixStudio`, importação única por origem sem sobrescrever arquivos existentes nem apagar originais; conflitos arquivados em `migration-backups`.
- Instalador Inno Setup por usuário, atalhos opcionais, verificação de .NET 4.8/compatível e driver. Desinstalação preserva AppData e ViGEmBus compartilhado.
- Driver ausente: tarefa desmarcada, confirmação explícita, download oficial 1.22.0 fixado por SHA-256. Sem atualizador antigo. .NET ausente: somente oferta de endereço oficial, sem runtime embutido.

## Distribuição e disco

| Item | Antes | Depois |
|---|---:|---:|
| ZIP portátil | 228.477 bytes | 115.675 bytes |
| Executável | 87.552 bytes | 93.696 bytes |
| Instalador, comparação controlada | 2.240.577 bytes | aproximadamente 2.131.656 bytes |
| Instalação incluindo desinstalador | 4.764.679 bytes | aproximadamente 4.626.557 bytes |

ZIP aproximadamente 49% menor. O executável cresceu cerca de 6 KB com migração, diagnóstico e ícone; a redução está no pacote completo. Payload: 239.922 bytes. Instalador: cerca de 2,03 MiB; instalação: 4,41 MiB, excluindo dados e pré-requisitos.

A versão 2.1 não tinha instalador publicado. A referência foi reconstruída com seu ZIP original e o mesmo Inno Setup/script/compressão. A comparação representa a alteração do payload. Espaço instalado foi medido na variante isolada de validação; o pacote de produção pode variar ligeiramente.

Limpeza: 73 arquivos/pastas gerados obsoletos, 4.493.573 bytes recuperados fora do backup. Dados de todas as cópias, pesquisa de engenharia reversa, fontes, licenças e pacote oficial da dependência preservados. Backup: `backups/pre-optimization-20261027-143519/workspace.zip`, bundle Git e cópia de AppData anterior à migração.

## Recursos

Ensaio local: quatro cenários, aproximadamente 2 s de aquecimento e 32 amostras de 250 ms. Fontes anteriores e posteriores compilados com a mesma instrumentação. CPU normalizada para um núcleo; máquina com 12 processadores lógicos. Não mede latência de jogo.

| Cenário | CPU antes/depois | RAM privada antes/depois | RAM residente antes/depois |
|---|---:|---:|---:|
| Janela ociosa | 2,13 / 1,33% | 27,20 / 27,72 MiB | 42,87 / 44,79 MiB |
| Ocioso minimizado | 1,55 / 1,34% | 31,32 / 31,29 MiB | 48,44 / 51,87 MiB |
| Motor neutro minimizado | 4,45 / 2,67% | 32,58 / 32,75 MiB | 50,93 / 54,50 MiB |
| Navegação repetida | 16,70 / 12,56% | 33,99 / 34,42 MiB | 56,47 / 60,32 MiB |

Nesta execução, CPU do motor neutro minimizado caiu aproximadamente 40%; na máquina inteira: 0,371% → 0,223%. Ambos enviaram 32 keepalives. RAM privada ficou próxima da referência; residente aumentou alguns MiB. Não há evidência de redução de RAM. Medições curtas sofrem influência do sistema. GDI ficou estável nos trechos ociosos, com pico de navegação 65 antes e 64 depois; não substitui teste prolongado de vazamentos.

Dados: [antes](measurements/runtime-before.json), [depois](measurements/runtime-after.json), [instalador](measurements/installer.json), [limpeza](measurements/cleanup.json).

Uma [repetição com os fontes finais](measurements/runtime-repeat.json) confirmou o motor neutro minimizado em 2,72% de um núcleo (cerca de 39% abaixo da referência), com 33,22 MiB privados e 56,24 MiB residentes. A janela ociosa marcou 1,52%; navegação marcou 21,44%, acima da referência. O cenário ocioso minimizado perdeu o estado minimizado e foi considerado inválido. Esses resultados mostram a variabilidade do ensaio: não se afirma redução geral de CPU em todos os cenários nem redução de RAM.

## Validação

- 44 verificações de lógica, persistência, migração e recursos aprovadas.
- Quatro de integração aprovadas: Xbox enumerado, botão/analógico lidos por XInput, soltura/neutro e relatórios DS4 aceitos pelo driver.
- Três adicionais com hooks reais aprovadas na janela temporária: ESPAÇO → A, soltura → neutro e mouse → analógico direito lido por XInput.
- 18 de instalação aprovadas: atualização, reinstalação, atalhos, preservação exata dos perfis, desinstalação e pré-requisitos ausentes simulados. Driver compartilhado permaneceu idêntico por hash.
- Perfil existente importado para AppData; arquivos originais preservados e conferidos por hash.

```powershell
./build.ps1 -Package -Installer
./.build/tests/KinetixStudio.Tests.exe
./.build/tests/KinetixStudio.Tests.exe --integration
# Precisa de desktop interativo e foco exclusivo na janela do teste.
./.build/tests/KinetixStudio.Tests.exe --input-integration
./scripts/test-installer.ps1
./scripts/measure-runtime.ps1 -OutputDir .build/measure -Report .build/measure.json
```

Aplicativo/ZIP compilam com a dependência local verificada. Para gerar o instalador, `get-inno.ps1` prepara Inno Setup oficial fixado por hash e assinatura. O compilador não é distribuído com o aplicativo. O teste usa AppId próprio e pasta única por execução. `-BaselineZip` aceita um ZIP 2.1 preservado; sem ele, testa reinstalação atual sem alegar comparação histórica.

## Limites

Nenhuma partida, anticheat ou sessão prolongada foi validada. PS4 confirma aceitação do relatório pelo driver, sem leitura independente HID. Ausência de .NET/driver foi simulada. O instalador oficial do driver foi obtido e validado, mas não instalado para preservar o driver existente. Aplicativo e instalador não têm assinatura de código própria. ViGEm foi arquivado e permanece dependência externa sem manutenção oficial.
