# Arquitetura do Kinetix Studio

Aplicação WinForms x64 para .NET Framework, sem servidor, telemetria ou conexão de rede durante o uso.

| Componente | Responsabilidade |
|---|---|
| `Models.cs` | Perfis JSON, validação, presets, gravação atômica e cálculo dos analógicos |
| `Native.cs / VirtualPad` | Ciclo de vida ViGEmClient, relatórios DS4 e Xbox |
| `Native.cs / InputEngine` | Hooks de entrada, fila de transições, foco permitido e atualização da saída |
| `App.cs` | Inicialização e prevenção de instâncias simultâneas |
| `MainWindow.cs / Dialogs.cs` | Estúdio, editor, perfis, diagnóstico, bandeja e diálogos |
| `Theme.cs / Brand.cs` | Componentes desenhados, controle ao vivo e identidade vetorial |
| `tests/Tests.cs` | Verificações de lógica, persistência e integração opcional por XInput |

## Ciclo da entrada

1. O hook verifica a janela em foco e a regra do perfil.
2. Teclas e botões atualizam um snapshot; transições são enfileiradas para não serem perdidas entre ciclos.
3. A roda produz um pulso digital de 60 ms; eventos repetidos criam novas transições.
4. O motor calcula WASD normalizado, caminhada opcional e mouse suavizado. Quando a suavização é maior que zero, `MouseVelocityWindow` regulariza deslocamentos numa janela ponderada de até 16 ms antes do filtro exponencial. Suavização zero mantém o caminho direto. A compensação de zona morta cresce continuamente perto do neutro; o corte mínimo é radial para preservar a direção.
5. Relatórios diferentes são enviados imediatamente no ciclo; estados iguais têm keepalive a cada 250 ms.
6. Pausa, perda de foco permitido e encerramento neutralizam a saída.

O temporizador usa uma geração por sessão e proteção contra execução concorrente. O lock protege os dados compartilhados e o ciclo nativo. O intervalo solicitado não é uma promessa de latência ou frequência efetiva; a interface informa ciclos medidos.

Desde 2.3, o multiplicador ADS é aplicado à velocidade do mouse antes da suavização, somente no analógico direito e enquanto a ação L2 estiver ativa. Depois da suavização, o filtro de ruído opcional atenua radialmente as amplitudes pequenas com uma transição contínua. O zero desativa o filtro; movimentos acima do limiar conservam a resposta anterior. Esses ajustes não consultam o jogo nem detectam alvos.

## Foco e cursor

O Kinetix Studio pausa a captura em sua própria interface, exceto na tela explícita de teste. Nesta tela, o cursor fica livre. Um filtro opcional de jogo compara o nome e o caminho completo do processo; a decisão é refeita ao mudar a janela em foco. Processos inacessíveis não são considerados compatíveis.

O mouse usa deslocamentos do cursor do Windows. Não há Raw Input, supressão de entrada em nível de driver, ocultação de dispositivos físicos ou promessa de funcionar em todo jogo. Jogos podem ler teclado e mouse por caminhos que hooks de usuário não bloqueiam.

## Dados

`StoragePaths.cs` usa `%LOCALAPPDATA%\KinetixStudio`. A pasta `data` adjacente é importada uma vez por origem, preservando os originais e os arquivos já existentes. Conflitos recebem cópia em `migration-backups`. O instalador aceita uma origem externa. `KINETIX_DATA_DIR` permite caminho absoluto para testes isolados. O pacote não inclui dados pessoais.

## Recursos e distribuição

Fontes são reutilizadas e liberadas ao encerrar. O ícone é clonado do recurso incorporado sem carregar componentes do Shell para extraí-lo. O temporizador visual para quando a janela fica oculta/minimizada ou fora das páginas ao vivo; o intervalo do motor continua independente. Estados neutros evitam alocar conjuntos vazios de botões.

Release usa csc x64 otimizado, sem símbolos e com alinhamento de 512 bytes. O payload contém somente quatro arquivos permitidos. Inno Setup gera instalação por usuário, verifica pré-requisitos e preserva AppData e drivers na desinstalação.
