# Ajustes de precisão — 2.3.0

Implementados em 29/10/2026 a partir da análise de um perfil `.rewasd` fornecido pelo usuário. A referência continha Xbox 360, curva personalizada, suavização, filtro de ruído e escalas diferentes por eixo. Os parâmetros internos dos dois programas não são equivalentes: não houve conversão automática nem execução/importação do arquivo original. Seu conteúdo e o texto fornecido não foram publicados no repositório.

## Uso

Em **Meus perfis → Presets prontos → Mira estável**, crie um perfil separado. Ele usa Xbox 360, sensibilidade 45, suavização 24 ms, curva 125%, compensação de zona morta 5%, filtro 10 e multiplicador ADS 60%. São valores iniciais ajustáveis, sem validação em uma partida específica.

Em **Mouse e mira**, role até os novos controles:

- **Filtro de ruído**: intensidade de 0 a 30. Zero desativa. Atenua oscilações pequenas com transição radial contínua, preservando a direção e o alcance máximo. Não representa porcentagem nem a mesma escala do `noiseFilter` do reWASD.
- **Sensibilidade ao mirar · LT/L2**: de 10 a 100%. Atua enquanto a ação L2 está pressionada, inclusive se mapeada a outra tecla. Só altera mouse no analógico direito; não reduz WASD nem mouse no esquerdo. 100% conserva a resposta normal. É aplicada antes da curva e suavização.
- **Proporção vertical**, já existente, permite ajustar Y independentemente de X.

O jogo pode aplicar sua própria sensibilidade ao mirar; os ajustes se combinam. Se a mira ficar lenta demais, mantenha o multiplicador do Kinetix em 100% e ajuste uma variável por vez. Alterar um ajuste pausa o remapeamento, seguindo o comportamento existente; F8 reativa e F12 desliga.

## Compatibilidade e validação

Perfis antigos recebem `NoiseFilter=0` e `AdsSensitivity=100`, sem alterar seu comportamento. Novos parâmetros são persistidos em JSON e validados ao importar. Os presets anteriores permanecem intactos. Backup dos fontes anteriores em `backups/precision-2.2.1/source-before.zip`.

- 67 verificações de lógica, incluindo 13 novas sobre filtro, ADS, remapeamento de L2, soltura, alcance, direção, persistência, limites e compatibilidade.
- Seis de integração com ViGEmBus: Xbox enumerado, botões/analógico, soltura, mouse com filtro/ADS lido por XInput, liberação do mouse/L2 e relatório DS4 aceito.
- Duas verificações da interface: sliders salvam em JSON e botão seleciona o novo preset; telas renderizadas e inspecionadas.
- 18 de instalação/atualização, preservando perfil original, preferências e driver compartilhado.

Relatórios: [instalação](measurements/precision-installer.json), [interface](measurements/precision-ui.json). O aplicativo estava aberto no workspace, portanto a versão nova foi compilada separadamente em `artifacts/precision`; a sessão existente não foi encerrada.

Não houve teste em uma partida de Fortnite nem medição de latência ponta a ponta nesta versão. A captura continua baseada no cursor do Windows, sujeita à sua aceleração e à interação com o jogo. Os novos controles não acrescentam rastreamento de alvos, automação de tiro, compensação de recuo, alteração da assistência nativa ou ocultação do software. Compatibilidade técnica não constitui autorização da Epic nem garantia contra punição.
