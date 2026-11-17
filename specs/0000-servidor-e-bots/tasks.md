# Tarefas da fatia 0

Este arquivo é a memória entre sessões: quem chegar depois lê aqui o que foi feito e continua daqui.

Legenda: `[ ]` pendente · `[~]` em andamento · `[x]` concluída · `[!]` bloqueada

- [x] T01: Base do protocolo em `Elysium.Core`: `EntityId`, leitor e escritor binários, envelope (tamanho + tipo) com tamanho máximo, leitura e escrita de envelopes num stream, `ProtocolException` (CA-08). Verificação: testes em `Elysium.Core.Tests` (ida e volta, dados truncados, envelope grande demais).
- [x] T02: Mensagens de login em `Elysium.Core` (`LoginRequest`, `LoginAccepted`, `LoginRejected`) e a versão 1 do protocolo (CA-01, CA-02). Verificação: testes de ida e volta; a versão é lida antes do resto.
- [x] T03: `Elysium.Classic.Shared`: `TilePosition`, mapa em grade (livre, bloqueado, pontos de entrada), leitura do texto do mapa com validação, mensagens de movimento e de visibilidade (cada lado decodifica pelo número do tipo, com `Frame.Decode<T>`; um decodificador central não foi necessário). Verificação: testes em `Elysium.Classic.Shared.Tests`.
- [x] T04: `Elysium.Classic.Server`: simulação do mapa com entrada, saída, pontos de entrada e AOI por células (CA-05, CA-06, CA-07). A interface `IClientOutbox` nasceu aqui, em `Elysium.Core.Server`, porque a simulação precisa dela. Os passos que levam um bot para dentro e para fora do raio (resto do CA-06) ficam na T05. Verificação: testes determinísticos em `Elysium.Classic.Server.Tests`.
- [x] T05: Movimento: caminho na grade (busca em largura com limite), um passo por intervalo da velocidade, recusa de destino inválido (CA-03, CA-04). Verificação: testes com ticks controlados.
- [x] T06: Ator do mapa (fila de entrada, decodificação, mensagem inválida desconecta só aquele jogador) e teste de determinismo (CA-08, CA-09). A fila (`Mailbox`), `ITickable` e `IWorld` nasceram aqui, em `Elysium.Core.Server`, porque o ator precisa deles. Verificação: testes em `Elysium.Classic.Server.Tests`.
- [x] T07: `Elysium.Core.Server`: testes da fila do ator e loop de ticks com `TimeProvider`; registrar `Microsoft.Extensions.Logging` em `Directory.Packages.props` (trava aprovada pelo dono em 2026-11-17). Verificação: testes em `Elysium.Core.Server.Tests`.
- [x] T08: Gateway TCP: conexões, sessões, login, repasse ao mundo, desconexão e mensagens inválidas (CA-01, CA-02, CA-07, CA-08). Verificação: testes com TCP local e um mundo falso.
- [x] T09: `Elysium.Core.Client`: conexão do cliente, login, envio e recebimento. Feita antes da T08, porque os testes do gateway usam esta conexão. Funciona sobre qualquer `Stream`, então o TLS entra depois sem mudar o resto. Verificação: usada pelos testes da T08 e da T11.
- [x] T10: `tools/Elysium.Bot`: visão do mundo, comandos `walk` e `demo`. Verificação: build e testes da T11.
- [x] T11: `samples/Elysium.Demo.Server` (mapa e números do jogo), inclusão em `Elysium.Architecture.Tests` (trava aprovada pelo dono em 2026-11-17) e teste de ponta a ponta com três bots (CA-10). Verificação: teste em `Elysium.Demo.Server.Tests`.
- [x] T12: Documentação: ARCHITECTURE, documento do módulo do servidor e do protocolo (`docs/modules/servidor-e-protocolo.md`), código de referência em CONVENTIONS, TECH_DEBT (TD-0001 a TD-0006). Verificação: `Elysium.Checks -- all`.

## Bloqueios

*(nenhum)*

## Revisão

Revisão independente feita pelo subagente `revisor` em 2026-11-17, a pedido do dono "como um sênior revisaria um júnior". Nenhum achado crítico. Todos os achados foram conferidos no código, e todos procediam.

| Achado | Gravidade | Procede? | O que foi feito |
|---|---|---|---|
| I1: um cliente podia congelar o mapa mandando pedidos válidos sem parar (fila de entrada sem limite) | importante | sim | `MapActorLimits`: limite de comandos por tick, só o último pedido de movimento de cada jogador vale, e quem enche a fila é desconectado. Testes novos em `MapActorTests`. Tempo máximo de login e limite de conexões viraram o TD-0007 |
| I2: um cliente que para de ler fazia a memória crescer e travava o desligamento | importante | sim | Fila de saída limitada por cliente (`MaxQueuedFramesPerClient`), escrita interrompível pelo desligamento e pela desconexão. Dois testes novos em `GatewayTests` |
| I3: protocolo e plano diferentes do aprovado (versão no login, não no envelope; sem relógio da simulação, ID do mapa nem RNG) | importante | sim | Documentado em `ProtocolVersion.cs` e no doc do módulo que o envelope e o começo do login nunca mudam. **Precisa da aprovação do dono no `/fechar`.** Os outros desvios vão para o report (lista abaixo) |
| I4: nenhum teste fixava os bytes do protocolo | importante | sim | Testes de bytes fixos para o envelope, cada mensagem e os números de tipo |
| M1: o gateway cria o `EntityId` | menor | sim | TD-0008 (fatia 2, com os monstros); o log deixou de chamar o ID de "connection" |
| M2: ordem das operações no gateway | menor | sim | O nome é liberado antes de o mundo saber da saída; o motivo do fechamento é gravado antes de fechar a fila. O `demo` espera cada bot entrar antes de conectar o próximo, então os pontos de entrada não dependem do tempo da rede. O `LoginAccepted` continua antes do `PlayerJoined`, de propósito: o cliente sempre recebe o aceite antes das mensagens do mapa |
| M3: controle das conexões no listener | menor | sim | A conexão é registrada antes de começar; erro inesperado vai para o log, com `try/finally` |
| M4: pausas fixas nos testes de rede | menor | sim | `DemoServer.OnlinePlayers` e espera por condição; a corrida do `GatewayTests` sumiu com a nova ordem (M2) |
| M5: marcas de CA em testes que não provam o critério | menor | sim | Marcas retiradas dos testes de unidade; os testes continuam iguais |
| M6: teste do loop de ticks com relógio real e asserção trivial | menor | sim | `ManualTimeProvider`: 1 segundo dá exatamente 10 ou 20 ticks |
| M7: o teste do CA-09 não pegava diferença entre processos | menor | sim | Hash gravado da sequência de mensagens; conferido igual em três processos |
| M8: o bot não tratava mensagem inválida do servidor | menor | sim | O bot para, avisa e se marca como desconectado |
| M9: itens provisórios faltando | menor | sim | TD-0007 e TD-0008; TD-0004 completado |
| E1: `MapSimulation` virando classe-deus | estilo | sim | Visibilidade extraída para `AreaOfInterest`; `PlanPath` e `TakeStep` com nomes; campos renomeados. O hash do CA-09 não mudou: as mensagens e a ordem são as mesmas |
| E2: `RunConnectionAsync` longo | estilo | sim | Dividido em `LoginAsync` e `PlayAsync` |
| E3: `GridMapText.Parse` longo | estilo | sim | Dividido em `ParseSpawns` e `ParseTiles` |
| E4: `WorldView` mistura estado com texto; bloqueios repetidos no bot | estilo | em parte | Bloqueios num único `ReadView`. A troca por mudanças tipadas fica para a mudança do `WorldView` para `Elysium.Classic.Client` (TD-0004, fatia 1) |
| E5: comando `walk` no `Program.cs` e argumentos sem validação | estilo | sim | `WalkCommand` e validação; argumento inválido mostra a mensagem de uso |
| E6: código repetido | estilo | sim | `Frame.FromMessage` substitui três cópias; `MoveRequest` e `MoveRejected` conferem o `writer` |
| E7: nomes e API | estilo | sim | `CoreMessageTypes` foi para `Elysium.Core.Protocol`; o parâmetro de versão só para testes saiu do `LoginAsync`; `LoginResult` não aceita estado impossível; `connectionNumber` |

Desvios da spec e do plano aprovados, para o report e para a decisão do dono (I3):

- A versão do protocolo está no primeiro campo do `LoginRequest`, não em cada envelope. Por isso o envelope e o começo do login ficam iguais em todas as versões.
- Não existe interface de "relógio da simulação": a simulação só conta ticks, e o `TickRunner` usa `TimeProvider`. Isso cumpre o INV-04 sem precisar da exceção de API proibida.
- Não existe RNG injetado (a spec cita "relógio e RNG injetados"): nada nesta fatia sorteia. Ele nasce com o primeiro sorteio (drops, na fatia 2).
- O gateway não envia comandos "pelo ID do mapa": com um mapa só, o mundo é o próprio `MapActor`. O ID do mapa entra com o segundo mapa.

## Notas para a próxima sessão

- Desenho adotado na implementação (diferente do plano; ver "Revisão"): a simulação só conhece o número do tick (sem relógio); o loop de ticks usa `TimeProvider` só na montagem. Por isso nenhum arquivo precisa da exceção de API proibida.
- Pontos de entrada: o jogador entra no ponto `jogadores no mapa % quantidade de pontos`. Com o servidor vazio, Ana, Bruno e Caio entram nos pontos 0, 1 e 2; o mapa de demonstração põe 0 e 1 perto e 2 longe.
- Faixas de tipo de mensagem: Núcleo 1–99, Classic 100–999, jogo 1000 em diante.
- Todas as tarefas concluídas e revisão feita. Próxima etapa: `/fechar`.
- Para o report: o plano falava em "envelope com tamanho, tipo e versão"; a versão ficou no primeiro campo do `LoginRequest`, não em cada envelope (evita 2 bytes por mensagem e é conferida antes de qualquer outra coisa). O efeito para o CA-02 é o mesmo.
- Para o report: o `MovesRefused` do bot foi acrescentado depois do teste manual, porque o `walk` para um tile bloqueado esperava 60 segundos à toa.
- Verificado à mão nesta sessão: servidor de demonstração + `demo` (PASSED, duas vezes no mesmo servidor) e `walk` (chegada e recusa). **Não verificado:** o desligamento com `Ctrl+C`, porque aqui o servidor foi parado à força; fica para o teste do dono (CA-11).
