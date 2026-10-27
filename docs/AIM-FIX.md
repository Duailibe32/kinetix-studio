# Correção da mira — 2.2.1

O relato recebido foi de mira imprecisa e trêmula. A reprodução local identificou dois comportamentos matemáticos, sem depender de um jogo específico:

- A compensação de zona morta era somada integralmente a qualquer movimento acima do corte mínimo. Com zona de 7%, um micromovimento saltava diretamente para mais de 7% de saída. O novo cálculo introduz essa compensação de forma contínua nos primeiros 5% de magnitude, mantendo o alcance total e a compensação original acima dessa faixa.
- Cortar cada eixo separadamente distorcia direções rasas: o eixo menor desaparecia mesmo enquanto o outro se movia. Agora o corte é radial e muito menor.

Além disso, os eventos de um mouse de 125 Hz podem chegar a cada 8 ms enquanto o motor executa a cada 4 ms. Uma janela ponderada pelo tempo, de até 16 ms, regulariza os ciclos intercalados sem entrada. É usada somente quando há suavização configurada, adicionando uma média curta ao filtro existente. Suavização zero mantém a resposta direta. O histórico é descartado ao iniciar, parar ou mudar o foco permitido.

Não foram alteradas sensibilidade, DPI, opções do Windows ou configurações dos perfis existentes. O mouse continua baseado no deslocamento do cursor do Windows; não houve migração para Raw Input. Aceleração do cursor, recentralização feita pelo jogo, zona morta e curva internas do jogo ainda podem afetar a experiência.

Dez regressões automatizadas cobrem: micromovimento sem salto, direção rasa, proporção diagonal, eventos de 125 Hz em ciclos de 250 Hz, parada da janela, suavização zero, intervalos irregulares, descarte de histórico, alcance completo e retorno ao neutro com janela e suavização juntas. O primeiro teste falhou antes da alteração e passou depois. O total de lógica passou de 44 para 54 verificações.

Os três testes com hooks reais passaram usando suavização de 30 ms, incluindo leitura do mouse pelo XInput. O retorno ao neutro foi conferido em sequência sintética controlada, pois esse ensaio no desktop compartilhado é afetado por movimento simultâneo do mouse. As 18 verificações de instalação/atualização passaram; relatório em [aim-installer.json](measurements/aim-installer.json).

Essa validação confirma o cálculo e o transporte para o controle virtual, sem reproduzir o jogo nem o hardware do amigo que relatou a falha. A correção deve ser conferida no mesmo cenário antes de considerar o relato resolvido. Backup dos fontes anteriores: `backups/aim-fix-2.2.0/source-before.zip`.
