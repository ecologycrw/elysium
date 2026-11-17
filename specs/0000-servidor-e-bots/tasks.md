# Tarefas da fatia 0

Este arquivo é a memória entre sessões: quem chegar depois lê aqui o que foi feito e continua daqui.

Legenda: `[ ]` pendente · `[~]` em andamento · `[x]` concluída · `[!]` bloqueada

- [x] T01: Base do protocolo em `Elysium.Core`: `EntityId`, leitor e escritor binários, envelope (tamanho + tipo) com tamanho máximo, leitura e escrita de envelopes num stream, `ProtocolException` (CA-08). Verificação: testes em `Elysium.Core.Tests` (ida e volta, dados truncados, envelope grande demais).
- [x] T02: Mensagens de login em `Elysium.Core` (`LoginRequest`, `LoginAccepted`, `LoginRejected`) e a versão 1 do protocolo (CA-01, CA-02). Verificação: testes de ida e volta; a versão é lida antes do resto.
- [x] T03: `Elysium.Classic.Shared`: `TilePosition`, mapa em grade (livre, bloqueado, pontos de entrada), leitura do texto do mapa com validação, mensagens de movimento e de visibilidade (cada lado decodifica pelo número do tipo, com `Frame.Decode<T>`; um decodificador central não foi necessário). Verificação: testes em `Elysium.Classic.Shared.Tests`.
- [x] T04: `Elysium.Classic.Server`: simulação do mapa com entrada, saída, pontos de entrada e AOI por células (CA-05, CA-06, CA-07). A interface `IClientOutbox` nasceu aqui, em `Elysium.Core.Server`, porque a simulação precisa dela. Os passos que levam um bot para dentro e para fora do raio (resto do CA-06) ficam na T05. Verificação: testes determinísticos em `Elysium.Classic.Server.Tests`.
- [x] T05: Movimento: caminho na grade (busca em largura com limite), um passo por intervalo da velocidade, recusa de destino inválido (CA-03, CA-04). Verificação: testes com ticks controlados.
- [ ] T06: Ator do mapa (fila de entrada, decodificação, mensagem inválida desconecta só aquele jogador) e teste de determinismo (CA-08, CA-09). Verificação: testes em `Elysium.Classic.Server.Tests`.
- [ ] T07: `Elysium.Core.Server`: fila do ator e loop de ticks com `TimeProvider`; registrar `Microsoft.Extensions.Logging` em `Directory.Packages.props` (trava aprovada pelo dono em 2026-11-17). Verificação: testes em `Elysium.Core.Server.Tests`.
- [ ] T08: Gateway TCP: conexões, sessões, login, repasse ao mundo, desconexão e mensagens inválidas (CA-01, CA-02, CA-07, CA-08). Verificação: testes com TCP local e um mundo falso.
- [ ] T09: `Elysium.Core.Client`: conexão do cliente, login, envio e recebimento. Verificação: usada pelos testes da T08 e da T11.
- [ ] T10: `tools/Elysium.Bot`: visão do mundo, comandos `walk` e `demo`. Verificação: build e testes da T11.
- [ ] T11: `samples/Elysium.Demo.Server` (mapa e números do jogo), inclusão em `Elysium.Architecture.Tests` (trava aprovada pelo dono em 2026-11-17) e teste de ponta a ponta com três bots (CA-10). Verificação: teste em `Elysium.Demo.Server.Tests`.
- [ ] T12: Documentação: ARCHITECTURE, documento do módulo do servidor e do protocolo, código de referência em CONVENTIONS, TECH_DEBT. Verificação: `Elysium.Checks -- all`.

## Bloqueios

*(nenhum)*

## Revisão

Achados do revisor e o que foi feito com cada um. Preenchido pelo `/revisar`.

## Notas para a próxima sessão

- Desenho combinado no plano: a simulação só conhece o número do tick (sem relógio); o loop de ticks usa `TimeProvider` só na montagem. Por isso nenhum arquivo precisa da exceção de API proibida.
- Pontos de entrada: o jogador entra no ponto `jogadores no mapa % quantidade de pontos`. Com o servidor vazio, Ana, Bruno e Caio entram nos pontos 0, 1 e 2; o mapa de demonstração põe 0 e 1 perto e 2 longe.
- Faixas de tipo de mensagem: Núcleo 1–99, Classic 100–999, jogo 1000 em diante.
