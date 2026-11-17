# Convenções

> Aqui fica só o que as ferramentas não verificam sozinhas. Formatação, nomes, avisos, APIs proibidas e dependências entre projetos são garantidos por `.editorconfig`, analisadores, testes de arquitetura e `Elysium.Checks`.

## Idiomas

| O quê | Idioma |
|---|---|
| Código, identificadores, comentários no código, mensagens de commit | inglês |
| Specs, planos, relatórios, ADRs, STATUS e demais docs do projeto | português |
| Documentação para quem usa a engine | decidido depois (provavelmente inglês) |

## Nomes (padrão .NET)

- **Projetos:** `Elysium.<Área>[.<Subárea>]`, em PascalCase com pontos. A pasta do projeto, o `.csproj`, a DLL e o namespace raiz têm o mesmo nome.
- **Subpastas de um projeto:** PascalCase sem pontos. Cada uma vira um pedaço do namespace.
- **Arquivos:** um tipo público por arquivo, com o nome do tipo. Interfaces começam com `I`.
- **Projetos de teste:** `<Projeto>.Tests`.
- **Pastas de organização do repositório:** minúsculas (`src/`, `docs/`, `tests/`).
- **Specs:** `specs/NNNN-nome-em-kebab-case/`. **ADRs:** `docs/adr/NNNN-nome-em-kebab-case.md`.

## Commits

Formato: `tipo(escopo): descrição curta em inglês`

- `tipo`: `feat`, `fix`, `docs`, `test`, `refactor`, `perf`, `build`, `ci` ou `chore`.
- `escopo`: o número da fatia (`0000`) ou a área (`prep`, `docs`, `ci`).
- Dentro de uma fatia, a descrição começa pela tarefa: `feat(0000): T03 add grid movement validation`.
- A linha `Co-Authored-By` do Claude fica no fim da mensagem.

## Padrões da casa

- **Atores e mensagens:** o estado de um mapa só muda dentro do ator dele; o resto do sistema envia mensagens.
- **Ações do jogador são comandos** que o servidor valida antes de aplicar.
- **Referência por ID**, e não por referência direta entre objetos de sistemas diferentes.
- **Transação em toda troca de dono de item.**
- **Conteúdo é dado:** definições de itens, monstros e afins vêm de dados validados.
- **A interface pensa no destino, a implementação pensa no agora.**
- `record` para mensagens e dados imutáveis.
- `async` só nas fronteiras (rede, banco, arquivos), nunca dentro do tick.
- Container de injeção de dependência só na montagem da aplicação, nunca dentro da simulação.
- Source generators em vez de reflexão em tempo de execução.

## Antipadrões proibidos

- Singletons e "managers" globais.
- Herança profunda.
- Uma interface para cada classe "por precaução", e o estilo de sistemas corporativos em geral (MediatR, AutoMapper, repositórios genéricos para tudo, camadas sem motivo).
- Abstração antes da necessidade.
- Estado mutável compartilhado entre mapas.
- Otimização sem medição.

## Régua de qualidade

O que um revisor sênior recusaria no Elysium, mesmo com o build e os testes verdes (proposta P-0002):

- **Função longa:** um método que faz várias etapas em sequência. Cada etapa vira um método com nome (exemplo: `Gateway.LoginAsync` e `PlayAsync`).
- **Classe com mais de uma responsabilidade:** se a descrição da classe precisa de "e também", ela se divide (exemplo: `MapSimulation` cuida de entrar, sair e andar; `AreaOfInterest`, de quem vê quem).
- **Código repetido:** na terceira cópia, vira um método só (ver a [regra de três](#regra-de-três)).
- **Tratamento de erro que esconde o problema:** `catch` que engole a exceção sem registrar nem reagir, ou que pega mais do que sabe tratar. Todo `catch` amplo diz no comentário por que é amplo.
- **Nome vago:** `Manager`, `Helper`, `Data`, `Info`, `Util`, `Handler`, `scratch`, `temp`. O nome diz o que a coisa é ou faz (`_pathBuffer`, não `_scratch`).
- **Teste que verifica a implementação e não o comportamento:** o teste confere o que o sistema entrega (as mensagens enviadas, a posição, os bytes do protocolo), não como ele faz por dentro. Teste de ida e volta não basta para protocolo: os bytes são fixados.

Uma parte é verificada por máquina: os analisadores de complexidade (CA1502, CA1505 e CA1506) usam os limites padrão do .NET e falham o build. Um código que passa de um limite é melhorado; o limite só é afrouxado com um motivo real aprovado pelo dono. O resto é conferido pelo revisor, na lista obrigatória dele.

## APIs proibidas na simulação

Os projetos `*.Server` e `*.Shared` não podem usar relógio do sistema, `Random`, locks, `Thread.Sleep` nem console (lista em `src/BannedSymbols.txt`; INV-03 e INV-04). O build falha se alguém usar.

A única exceção é o arquivo que implementa o relógio real ou o RNG real do servidor. Ele usa `#pragma warning disable RS0030` com um comentário explicando o motivo. O revisor confere todo `#pragma` desse tipo, e nenhum outro arquivo pode ter um.

## Regra de três

Uma abstração só nasce quando o mesmo problema aparece pela terceira vez. Na primeira vez, resolve-se direto. Na segunda, nota-se a repetição. Na terceira, abstrai-se, já sabendo o que de fato se repete. A regra vale também para criar projetos novos.

## Código de referência

A IA imita o código que já existe. Por isso, os melhores exemplos de cada padrão são listados aqui conforme surgem (por exemplo: "para criar um novo tipo de mensagem, siga o arquivo X").

| Para... | Siga |
|---|---|
| criar uma mensagem de rede | `src/framework/Elysium.Classic.Shared/Messages/EntityMoved.cs` (registre o tipo em `ClassicMessageTypes` e fixe os bytes num teste, como em `ClassicMessageTests`) |
| criar um ator com fila e tick (com limite de trabalho por tick e por jogador) | `src/framework/Elysium.Classic.Server/Maps/MapActor.cs` |
| escrever regras de simulação determinísticas | `src/framework/Elysium.Classic.Server/Maps/MapSimulation.cs`, que delega a visibilidade a `AreaOfInterest.cs` |
| testar a simulação com ticks controlados | `tests/Elysium.Classic.Server.Tests/Maps/MapMovementTests.cs` |
| provar determinismo entre processos | `tests/Elysium.Classic.Server.Tests/Maps/MapActorTests.cs` (hash gravado da sequência de mensagens) |
| testar algo que depende de tempo real | `tests/Elysium.Core.Server.Tests/Actors/TickRunnerTests.cs`, com o `ManualTimeProvider` |
| testar algo pela rede de verdade | `tests/Elysium.Core.Server.Tests/Sessions/GatewayTests.cs` |
| escrever mensagens de log | `src/core/Elysium.Core.Server/Sessions/Gateway.cs` (`[LoggerMessage]`) |
| falar com o servidor sem travar o quadro do cliente | `src/core/Elysium.Core.Client/Connection/ServerLink.cs` |
| mostrar no cliente uma mensagem nova do servidor | `src/framework/Elysium.Classic.Client/World/WorldState.cs` (devolver um `WorldChange`) e `Presentation/ClientWorld.cs` (repassar a quem mostra) |
| desenhar algo novo na cena | `src/framework/Elysium.Classic.Client/Presentation/WorldSceneBuilder.cs`, com cores e tamanhos no `SceneStyle` do jogo |
| provar que o quadro não cria memória nova | `tests/Elysium.Classic.Client.Tests/Presentation/FrameAllocationTests.cs` (mensagens codificadas antes de medir) |
| testar o cliente de ponta a ponta, sem janela | `tests/Elysium.Demo.Client.Tests/EndToEndTests.cs`, com o `HeadlessClient` |

## Testes

- O framework é o xUnit v3. O nome do teste descreve o comportamento: `Move_rejects_step_faster_than_speed`.
- Um teste que prova um critério de aceitação recebe `[Trait("CA", "NNNN-CA-xx")]`.
- Sorteios e tempo nos testes vêm do RNG e do relógio injetados, nunca do sistema.
- Um teste nunca é apagado ou enfraquecido para "fazer passar".

## Documentação

- Curta. Aponta para outro documento em vez de repetir o que está nele.
- Diagramas Mermaid quando ajudarem:

| Tipo | Para quê |
|---|---|
| `flowchart LR` | fluxos e processos |
| `flowchart TB` | camadas e hierarquias |
| `sequenceDiagram` | quem fala com quem, e em que ordem |
| `stateDiagram-v2` | estados e transições |
| `erDiagram` | tabelas do banco |

- No máximo cerca de 15 elementos por diagrama, uma ideia por diagrama.
- O diagrama complementa o texto: toda regra importante também fica escrita.
- O que puder ser gerado a partir do código é gerado, não desenhado à mão.

## Scripts

- Hooks do Git em `sh` (rodam no Windows, pelo Git, e no Linux).
- Hooks do Claude Code em PowerShell 5.1, salvos em UTF-8 com BOM.

## Conteúdo de terceiros

- Nada de nomes, assets, textos, arquivos ou protocolos de outros jogos. Mapas, itens, monstros, eventos e textos são originais.
- Assets externos só com licença compatível (CC0, CC-BY com atribuição, MIT), registrada junto do asset.
- Código de outros projetos só com licença compatível com a MIT e com atribuição.
