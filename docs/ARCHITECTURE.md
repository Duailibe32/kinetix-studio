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
4. O motor calcula WASD normalizado, caminhada opcional e mouse suavizado.
5. Relatórios diferentes são enviados imediatamente no ciclo; estados iguais têm keepalive a cada 250 ms.
6. Pausa, perda de foco permitido e encerramento neutralizam a saída.

O temporizador usa uma geração por sessão e proteção contra execução concorrente. O lock protege os dados compartilhados e o ciclo nativo. O intervalo solicitado não é uma promessa de latência ou frequência efetiva; a interface informa ciclos medidos.

## Foco e cursor

O Kinetix Studio pausa a captura em sua própria interface, exceto na tela explícita de teste. Nesta tela, o cursor fica livre. Um filtro opcional de jogo compara o nome e o caminho completo do processo; a decisão é refeita ao mudar a janela em foco. Processos inacessíveis não são considerados compatíveis.

O mouse usa deslocamentos do cursor do Windows. Não há Raw Input, supressão de entrada em nível de driver, ocultação de dispositivos físicos ou promessa de funcionar em todo jogo. Jogos podem ler teclado e mouse por caminhos que hooks de usuário não bloqueiam.

## Dados

A pasta `data` fica ao lado do executável. Perfis antigos são compatíveis e recebem os padrões novos na desserialização. O ZIP de distribuição não inclui dados pessoais. Não há migração automática de diretórios externos: copie a pasta `data` da versão anterior, se estiver usando outra pasta de instalação.
