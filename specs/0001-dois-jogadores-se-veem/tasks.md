# Tarefas da fatia 1

Este arquivo é a memória entre sessões: quem chegar depois lê aqui o que foi feito e continua daqui.

Legenda: `[ ]` pendente · `[~]` em andamento · `[x]` concluída · `[!]` bloqueada

- [x] T01: Testes de mutação (trava, P-0003): Stryker.NET 5.1.0 como ferramenta local (`dotnet-tools.json`, na raiz), `stryker-config.json` em `tests/Elysium.Core.Server.Tests` e `tests/Elysium.Classic.Server.Tests`, job `mutation` na CI que falha abaixo de 80% (CA-10). Testes novos de comportamento no gateway (motivo da saída no log, conexão liberada na hora, limites do nome, configuração impossível recusada). Modo preview registrado no TD-0009; o que fazer se o job falhar e o passo para torná-lo obrigatório estão em `docs/setup/DEV.md` e `docs/setup/GITHUB.md`. Verificação: notas medidas localmente (`Elysium.Core.Server` 80,95%, `Elysium.Classic.Server` 82,00%); job verde na CI do PR.
- [x] T02: Protótipo gráfico: `Elysium.Core.Client.Sdl` com uma janela e um cubo na GPU do SDL3 (`ppy.SDL3-CS` em `Directory.Packages.props`, trava aprovada em 2026-11-17); conferir a biblioteca nativa no pacote, a compatibilidade com NativeAOT e a ferramenta de shaders; escrever o ADR 0012 (`proposta`). **Parada:** o dono confirma que a janela abriu com o cubo e aprova o ADR 0012 e a ferramenta de shaders. Verificação: build verde; o dono confirma a janela.
- [x] T03: `Elysium.Classic.Client` com o estado do mundo (o `WorldView` do bot) devolvendo mudanças tipadas; o bot formata o próprio texto; resolve o TD-0004 (CA-09). Verificação: testes em `Elysium.Classic.Client.Tests`; os testes de ponta a ponta dos bots continuam passando.
- [x] T04: `Elysium.Core.Client`: ligação com o servidor em segundo plano (leitura e escrita), fila de mensagens tirada a cada quadro e aviso de conexão caída (CA-06). Verificação: testes em `Elysium.Core.Client.Tests`.
- [x] T04b: Corrigir a P-0004 (trava, aprovada pelo dono em 2026-11-17): o `EngineModel` ignora dependências sem namespace, que nunca são tipos do Elysium; se a leitura de um tipo falhar por outro motivo, o erro diz qual tipo foi; testes que provam as duas coisas; o `ServerLink` volta para o `Task.WhenAll`; decisão registrada na P-0004. Verificação: testes em `Elysium.Architecture.Tests`.
- [ ] T05: Movimento suave em `Elysium.Classic.Client`: tiles confirmados, posição desenhada na velocidade do jogo, nunca além do confirmado, alcance quando atrasado (CA-03). Verificação: testes com tempo controlado.
- [ ] T06: Câmera 3/4 e clique para tile em `Elysium.Classic.Client` (CA-02). Verificação: testes com várias posições de câmera e tamanhos de janela.
- [ ] T07: Descrição da cena em `Elysium.Core.Client` e montagem da cena em `Elysium.Classic.Client` (chão, tiles bloqueados, personagens, marca de destino com recusa); teste de alocação por quadro (CA-04, CA-07). Verificação: testes em `Elysium.Classic.Client.Tests`.
- [ ] T08: `Elysium.Core.Client.Sdl` desenha a cena (instancing, luz direcional) e traduz mouse e janela para a entrada do núcleo; regra de arquitetura "só `Elysium.Core.Client.Sdl` usa o SDL" (trava aprovada em 2026-11-17) (CA-08). Verificação: teste de arquitetura; build verde.
- [ ] T09: `samples/Elysium.Demo.Shared` (mapa e velocidade de um passo), `Elysium.Demo.Server` passa a usá-lo, e `samples/Elysium.Demo.Client` (nome pela linha de comando, login, loop, FPS e conexão caída no título) (CA-01, CA-06). Verificação: testes em `Elysium.Demo.Client.Tests`; os testes do servidor continuam passando.
- [ ] T10: De ponta a ponta: servidor de demonstração e dois clientes sem janela pela rede; um clica, o outro vê andar até o tile certo; destino recusado; um sai e some (CA-02, CA-04, CA-05). Verificação: teste em `Elysium.Demo.Client.Tests`, na CI em Windows e Linux.
- [ ] T11: Documentação: ARCHITECTURE, documento do módulo do cliente, código de referência no CONVENTIONS, TECH_DEBT (três itens novos, TD-0004 resolvido) e VISION ("confirmada na fatia de escala", aprovada em 2026-11-17). Verificação: `Elysium.Checks -- all`.

## Bloqueios

*(nenhum)*

## Revisão

Achados do revisor e o que foi feito com cada um. Preenchido pelo `/revisar`.

## Notas para a próxima sessão

- Paradas previstas para o dono: na T01, se o Stryker não funcionar; no fim da T02, a janela com o cubo e a aprovação do ADR 0012 e da ferramenta de shaders.
- T01 (2026-11-17): o manifesto `dotnet-tools.json` ficou na raiz, porque o SDK 10 o cria ali (o plano dizia `.config/`). O Stryker roda dentro da pasta de testes (`dotnet stryker`). Nota inicial do núcleo: 58,3%; o dono escolheu melhorar os testes, pedindo testes de comportamento que importa, e não só para "matar mutantes". Os 16 sobreviventes que restam no núcleo não são observáveis por comportamento: opções de desempenho dos `Channel`, `Dispose` e `Stop`, a exceção do `Mailbox` fechado (código que nunca roda, porque a fila nunca é fechada) e dois motivos de fechamento que nunca vão para o log (login recusado e saída antes do login). A nota do núcleo ficou perto do limite (80,95%): um código novo sem teste no núcleo deixa o job vermelho.
- T02 (2026-11-17): concluída. O dono abriu a janela com o cubo e aprovou o ADR 0012 (agora `aceita`) e o DXC. O `Elysium.Demo.Client` é, por enquanto, só o programa do protótipo; vira o cliente de verdade na T09. O `SdlGpuRenderer.DrawCube` é substituído pelo desenho da cena na T08. Shaders: editar o HLSL e rodar `sh src/core/Elysium.Core.Client.Sdl/Shaders/compile.sh` (TD-0010).
- T03 (2026-11-17): `WorldState` em `Elysium.Classic.Client/World`; `Apply` devolve `WorldChange?` (struct, null quando nada mudou) e `WorldState.Handles` diz se o tipo é do mundo. O bot formata o texto em `BotNarration`. TD-0004 resolvido e retirado do TECH_DEBT (registrar no report). **Para a T07:** as mensagens (`EntityMoved` etc.) são `record class`, então `Frame.Decode` cria um objeto por mensagem. Para o CA-07 valer também para "aplicar as mensagens", talvez seja preciso tornar as mensagens frequentes `record struct` (os bytes não mudam) ou medir só depois da decodificação; decidir e, se for desvio da spec, perguntar ao dono.
- T04 (2026-11-17): `ServerLink` em `Elysium.Core.Client/Connection`: recebe numa fila e envia por um canal, em segundo plano; `TryTakeMessage` e `Send` nunca esperam; `IsConnected` e `LostReason` dizem quando e por que a conexão caiu. O `HeadlessBot` ainda tem o próprio loop de recepção (segunda ocorrência do mesmo problema; na terceira, unificar). O dono já tornou o check `mutation` obrigatório.
- T04b (2026-11-17): P-0004 corrigida, a pedido do dono, com commit próprio. `EngineModel.ForbiddenDependencies` aceita um modelo qualquer (para os testes), pula dependências sem namespace e, se falhar, diz qual tipo. Provado em `EngineModelTests` com `Fixtures/InlineArrayFixture.cs`. O `ServerLink` voltou para o `Task.WhenAll`.
- Próxima: T05 (movimento suave em `Elysium.Classic.Client`).
