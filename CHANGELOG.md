# Histórico

## 2.3.0 — Ajustes de precisão · 29/10/2026

- Filtro de ruído radial configurável, com transição contínua e alcance completo preservado.
- Sensibilidade separada para mouse no analógico direito enquanto LT/L2 está pressionado, respeitando teclas remapeadas.
- Preset adicional Mira estável, com Xbox 360, filtro e multiplicador de mira ajustáveis.
- Perfis anteriores mantêm filtro desligado e multiplicador de mira em 100%, preservando o comportamento existente.
- 67 verificações de lógica, seis de integração, duas da interface e 18 de instalação/atualização aprovadas.

## 2.2.1 — Estabilidade da mira · 27/10/2026

- Compensação da zona morta entra gradualmente nos micromovimentos, eliminando o salto instantâneo para 5–7% do analógico.
- Removido o recorte independente dos eixos que deformava movimentos quase horizontais ou verticais.
- Janela ponderada de até 16 ms regulariza os intervalos entre eventos de mouse quando há suavização; suavização zero mantém resposta direta.
- Janela reiniciada ao pausar, reiniciar ou trocar o foco permitido, sem reaproveitar movimentos antigos.
- Perfis e configurações mantidos. Dez testes adicionais de regressão; atualização e emulação revalidadas.

## 2.2.0 — Otimização e instalação · 27/10/2026

- Release x64 otimizado; distribuição restrita a quatro arquivos, sem testes e ferramentas.
- Instalador Inno Setup por usuário, com atalhos e desinstalação que preserva dados e driver compartilhado.
- Detecção de .NET 4.8 compatível; driver oficial opcional, verificado por SHA-256 e confirmação explícita.
- Perfis em AppData, importação não destrutiva de dados portáteis e cópia de conflitos.
- Menos atualizações visuais ociosas, fontes reutilizadas, ícone incorporado e menos alocações neutras.
- Descarte explícito do ícone e menu da bandeja; callbacks protegidos contra janela encerrada.
- Testes adicionais de migração, recursos, entrada e instalação; medições detalhadas em docs/OPTIMIZATION.md.

## 2.1.0 — Kinetix Studio · 27/10/2026

- Novo nome oficial: Kinetix Studio; executável KinetixStudio.exe.
- Logo geométrica em K, ícone, banner, telas e documentação atualizados.
- Repositório privado renomeado, preservando histórico e releases anteriores.
- Perfis, seleção e preferências continuam na mesma pasta data, com o mesmo formato.
- Histórico de eventos anterior copiado sem apagar o original; cópia existente não é sobrescrita.
- Proteção contra execução simultânea com a versão anterior.
- Nenhuma alteração nos mapeamentos ou no motor de controle virtual.
- Restauração da dependência corrigida para compilar repetidamente na mesma sessão sem tentar sobrescrever uma DLL carregada.

## 2.0.0 — Veylora · 27/10/2026

- Nova marca, logo vetorial, ícone, banner e estúdio em coral e azul-gelo.
- Interface reorganizada, navegação lateral, busca de ações, combos desenhados e diagnóstico.
- Presets de precisão FPS, exploração e corrida.
- Roda do mouse como entrada digital com soltura automática.
- Modificador de caminhada configurável, intensidade de caminhada e proporção vertical.
- Mouse configurável para analógico direito ou esquerdo.
- Intervalo de atualização de 4, 8 ou 16 ms, com frequência real medida.
- Bandeja do sistema com ativação, pausa, desligamento e opção de minimizar.
- Comparação de relatórios evita envios idênticos; há manutenção de conexão a cada 250 ms.
- Cache por janela evita consultar o processo inteiro a cada evento. Filtro de jogo confere o caminho completo.
- Temporizador protegido contra sobreposição e callbacks de uma sessão anterior.
- Escrita atômica com arquivos temporários únicos; identificador validado antes de excluir perfil.
- Perfis da versão anterior recebem padrões para os novos campos.
- Build offline restaura a DLL do pacote oficial versionado e verifica os SHA-256.
- Testes automatizados ampliados e pipeline Windows de compilação, testes e pacote portátil.

## 1.0.0 — Auralis

Primeira versão: teclado e mouse para controles virtuais PS4/Xbox, perfis JSON, editor de mapeamentos e atalhos F8/F12.
