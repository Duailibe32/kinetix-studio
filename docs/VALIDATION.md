# Revisão e validação — Kinetix Studio 2.3.0

Revisão 2.3.0: 67 testes de lógica e seis de integração com o driver passaram, incluindo leitura independente por XInput dos ajustes de filtro/ADS e liberação para neutro. Duas verificações da interface confirmaram persistência dos sliders e seleção do preset, com renderização inspecionada. As 18 verificações de instalação/atualização passaram. Veja [ajustes de precisão](PRECISION.md). As seções seguintes registram as versões anteriores.

Revisão 2.2.1: 54 verificações de lógica, quatro de integração com driver, três de hooks reais com suavização e 18 de instalação/atualização. Veja [correção da mira](AIM-FIX.md). Os números da seção seguinte são o registro histórico da versão 2.2.0.

Revisão atual: 44 verificações de lógica, quatro de integração, três de hooks reais e 18 de instalação aprovadas. Perfis existentes importados para AppData com conferência dos originais por hash. Veja [otimização](OPTIMIZATION.md) para medições, metodologia, comandos e limites. A seção abaixo registra a revisão anterior 2.1; os testes atuais ampliam essa cobertura.

Data: 27/10/2026. Plataforma: Windows x64.

## Resultado

- Compilação normal e compilação em pasta isolada concluídas.
- **35 verificações de lógica**, incluindo preservação de perfis e migração de histórico, aprovadas.
- **4 verificações de integração** com o driver instalado aprovadas.
- As sete telas foram renderizadas e inspecionadas.
- Os testes interativos de conexão ao driver, F8/F12, seletores e busca foram feitos na versão 2.0; a versão 2.1 mantém esse motor e esses fluxos, com atualização de marca e nova renderização das sete telas.

## O que os testes cobrem

Mapeamento ESPAÇO → Cross; codificação DS4; soltura e neutro; diagonais e direções opostas; mouse e inversão; gatilhos; tamanhos ABI; serialização e escrita atômica; reservas de teclas; validação de importação; caminhada lenta; roda como entrada; seleção de analógico; proporção vertical; presets; compatibilidade com perfis 1.0; limites de intervalo; proteção contra caminhos em identificadores; comparação de relatórios sem descartar a soltura; direcional diagonal.

A integração cria um Xbox virtual, identifica-o independentemente via XInput, lê A e analógico para cima, confirma o neutro, remove-o e cria um PS4 para enviar Cross e neutro. O teste PS4 confirma aceitação pelo driver; não é uma leitura independente de HID por um jogo.

## Melhorias revisadas

- Proteção contra callbacks antigos após reinício e contra ciclos simultâneos.
- Consulta de processo reutilizada por janela, com comparação do caminho completo do jogo.
- Envios idênticos reduzidos com keepalive; desenho do controle apenas quando seu estado muda.
- Perfis antigos preservados, sem ativar caminhada por ALT involuntariamente.
- Perfis locais com limite de tamanho, validação de identificador e escrita atômica.
- Dependência oficial verificada por SHA-256 e busca de DLL restrita à pasta do aplicativo e ao Windows.
- Nenhuma senha, token ou chave incorporada ao código.
- Logs, perfis, capturas de engenharia reversa e caminhos locais não fazem parte da publicação.
- Nenhum driver ou serviço pago foi instalado. Dados existentes não foram apagados.

## Atenção e limites

Nenhum jogo específico foi aberto nesta revisão. Bloqueio de entradas em jogos que usam outros caminhos de leitura, anticheat, captura em processos elevados e diferentes configurações de múltiplos monitores precisam de validação no ambiente de uso.

Importação/exportação de arquivos foi verificada no nível de validação/serialização, sem percorrer todos os diálogos do sistema por automação. A roda foi verificada como entrada no motor de mapeamento; ainda deve ser testada em uma partida real. A conferência interativa do último botão no monitor pode sofrer interferência de entrada simultânea do usuário; não substitui os testes independentes de XInput.

O aplicativo é Windows desktop, também redimensionável em tablets Windows. Não há suporte Android/iOS, banco SQL, API remota ou autenticação a otimizar. Dados são JSON local. Não há promessa de reproduzir todas as funções do reWASD.
