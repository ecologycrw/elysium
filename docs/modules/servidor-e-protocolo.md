# Módulo: servidor, mapa em grade e protocolo

> Documento vivo. Nasceu na fatia 0 e é atualizado no mesmo PR que muda o sistema.

## Responsabilidade

Recebe conexões, faz o login, roda cada mapa como um ator por ticks e decide sozinho o movimento e quem vê quem. Ainda **não** guarda nada em banco, não tem contas, combate, itens nem vários mapas.

## Onde fica

| Projeto | Camada | Papel |
|---|---|---|
| `Elysium.Core` | Núcleo | `EntityId`; protocolo: envelope, leitor e escritor binários, versão; mensagens de login |
| `Elysium.Core.Server` | Núcleo | fila do ator (`Mailbox`), loop de ticks (`TickRunner`), gateway e sessões, transporte TCP |
| `Elysium.Core.Client` | Núcleo | `GameConnection`: conectar, login, enviar e receber |
| `Elysium.Classic.Shared` | Classic | `TilePosition`, `GridMap`, formato de texto do mapa, mensagens de movimento e visibilidade |
| `Elysium.Classic.Server` | Classic | `MapActor` e `MapSimulation`: entrada, saída, caminho, movimento e AOI |
| `Elysium.Demo.Server` | Jogo | o mapa e os números do jogo de demonstração, e o programa do servidor |
| `Elysium.Bot` | Ferramenta | bot headless, com os comandos `walk` e `demo` |

## Conceitos

- **Tick:** o `TickRunner` chama `Tick()` do ator de 10 a 20 vezes por segundo. A simulação só conhece o número do tick; ela nunca lê um relógio (INV-04).
- **Ator do mapa:** a rede só coloca comandos na fila do `MapActor`. No tick, ele aplica os comandos na ordem de chegada e depois avança a `MapSimulation` (INV-03).
- **Gateway:** lê as mensagens de cada conexão fora do tick. A primeira tem de ser o login; depois, tudo vai para o mundo (`IWorld`). As respostas voltam pela `SessionRegistry` (`IClientOutbox`), por ID de entidade.
- **Pontos de entrada:** o jogador entra no ponto `jogadores no mapa % quantidade de pontos`.
- **Movimento:** o cliente pede um destino. O servidor recusa destino fora do mapa, bloqueado ou inalcançável, acha o caminho mais curto em 8 direções (sem cortar quina de parede, preferindo a linha reta) e anda um tile a cada `TicksPerStep` ticks. Pedir de novo não acelera.
- **AOI:** o mapa é dividido em células do tamanho do raio de visão. Ver é simétrico e vale até `ViewRadius` tiles, contando a diagonal como um.

## Protocolo, versão 1

Envelope, em little-endian: **2 bytes** com o tamanho do resto, **2 bytes** com o tipo da mensagem e o conteúdo. Strings: 1 byte de tamanho + UTF-8. Posições: coluna e linha em 2 bytes cada. A versão do protocolo é o primeiro campo do `LoginRequest`, e o servidor a confere antes de ler o resto.

| Tipo | Mensagem | Sentido | Campos |
|---|---|---|---|
| 1 | `LoginRequest` | cliente → servidor | versão, nome |
| 2 | `LoginAccepted` | servidor → cliente | entidade do jogador |
| 3 | `LoginRejected` | servidor → cliente | motivo: versão incompatível, nome inválido, nome em uso |
| 100 | `MoveRequest` | cliente → servidor | destino |
| 101 | `MoveRejected` | servidor → cliente | destino, motivo: fora do mapa, bloqueado, inalcançável |
| 102 | `EnteredMap` | servidor → cliente | entidade do jogador, posição |
| 103 | `EntityAppeared` | servidor → cliente | entidade, nome, posição |
| 104 | `EntityMoved` | servidor → cliente | entidade, posição (também para o próprio jogador) |
| 105 | `EntityDisappeared` | servidor → cliente | entidade |

Faixas de tipo: núcleo 1–99, Classic 100–999, jogos 1000 em diante. Qualquer mensagem inválida (antes do login, maior que o limite, truncada, de tipo desconhecido) fecha só aquela conexão.

## Formato do mapa

```text
# comentários e linhas em branco antes dos tiles são ignorados
spawn 10 10
tiles
....#....
```

`.` é livre e `#` é bloqueado; todas as linhas têm a mesma largura. O mapa do jogo de demonstração fica em `samples/Elysium.Demo.Server/Data/demo.map`.

## Como usar

O servidor de demonstração monta tudo em `samples/Elysium.Demo.Server/DemoServer.cs`. Os números do jogo ficam em `DemoGame.cs`.

## Invariantes relevantes

- INV-01: o cliente só pede o destino; o servidor calcula o caminho e o ritmo.
- INV-03 e INV-04: estado do mapa só muda dentro do tick; sem relógio nem `Random` na simulação; ordem estável por ID.
- INV-10: o raio de visão, a velocidade, o limite de caminho, o tamanho máximo de mensagem e de nome vêm do jogo.
- INV-11: versão conferida no login.

## Testes

- `tests/Elysium.Core.Tests`: envelope, leitor e escritor, mensagens de login.
- `tests/Elysium.Classic.Shared.Tests`: mapa em texto e mensagens do Classic.
- `tests/Elysium.Classic.Server.Tests`: entrada, saída, AOI, movimento, caminho, ator e determinismo, com ticks controlados.
- `tests/Elysium.Core.Server.Tests`: fila, loop de ticks e gateway com TCP local.
- `tests/Elysium.Demo.Server.Tests`: o cenário dos três bots pela rede.

## Itens provisórios

TD-0001 a TD-0006 em [TECH_DEBT.md](../TECH_DEBT.md).
