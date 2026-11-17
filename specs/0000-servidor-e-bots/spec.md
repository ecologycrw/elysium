---
id: 0000
titulo: Servidor e bots
status: em-andamento
aprovada-em: 2026-11-17
---

# Fatia 0: Servidor e bots

## Resumo para o dono

O servidor do jogo passa a existir de verdade: ele roda por ticks, aceita conexões pela rede, deixa personagens entrarem num mapa em grade e decide sozinho como cada um anda e quem vê quem. Para testar sem gráficos, nasce também o **bot**, um cliente sem tela que se conecta, anda e conta o que está vendo. No fim, três bots provam o básico de um MMORPG: dois perto se veem, o terceiro, longe, não vê ninguém, até chegar perto.

## Escopo

- **Servidor por ticks:** um mapa roda como um ator, num ritmo configurável entre 10 e 20 ticks por segundo, com relógio e RNG injetados.
- **Rede:** conexões TCP, mensagens com tamanho máximo e **versão 1 do protocolo**, conferida logo na entrada.
- **Login simples:** o bot informa a versão do protocolo e um nome de personagem; o servidor aceita ou recusa com um motivo. Ainda sem conta e senha.
- **Mapa em grade:** um mapa do jogo de demonstração, com tiles livres e bloqueados e um ponto de entrada, carregado de um arquivo de dados.
- **Movimento autoritativo:** o bot pede um destino; o servidor calcula o caminho pela grade e move o personagem um tile por vez, na velocidade definida pelos dados. O bot nunca diz onde está; só pede.
- **Área de interesse (AOI):** cada personagem só recebe o que acontece dentro do seu raio de visão: quem apareceu, quem andou e quem sumiu.
- **Bot headless** (`tools/Elysium.Bot`): conecta, faz login, anda e mostra no terminal o que vê. Tem um modo `demo`, que roda o cenário dos três bots.
- **Servidor do jogo de demonstração** (`samples/Elysium.Demo.Server`): o programa que o dono roda para ver tudo funcionando.

## Fora do escopo

- Gráficos, janela, câmera e clicar para andar: fatia 1.
- Contas, senhas, personagens salvos e banco de dados: fatia 2.
- Monstros, combate, itens e inventário: fatia 2.
- Vários mapas, teleporte e lease de posse: numa fatia futura, quando houver o segundo mapa.
- Criptografia da conexão (TLS): antes de qualquer teste aberto ao público.
- Prioridade e limite de entidades na AOI e o cenário de cerco: fatias de escala.
- Recarregar o mapa com o servidor rodando.

## Critérios de aceitação

- CA-01: Um bot com a versão certa do protocolo e um nome válido entra no mapa e recebe o seu número de entidade e a sua posição de entrada.
- CA-02: Um bot com outra versão do protocolo é recusado com o motivo "versão incompatível", e a conexão é fechada. Um nome vazio, longo demais ou já em uso também é recusado, com o motivo.
- CA-03: Um bot pede um destino livre e alcançável; o servidor move o personagem um tile por vez, nunca mais rápido do que a velocidade dos dados, até chegar ao destino.
- CA-04: Um destino bloqueado, fora do mapa ou inalcançável é recusado, e o personagem não sai do lugar. Nenhum passo do personagem cai num tile bloqueado.
- CA-05: Dois bots dentro do raio de visão um do outro se veem aparecer, com a posição correta, e cada um vê os passos do outro.
- CA-06: Um terceiro bot, fora do raio de visão, não recebe nada sobre os outros dois. Quando ele anda até perto, os dois aparecem para ele (e ele para os dois); quando ele se afasta, eles somem.
- CA-07: Quando um bot se desconecta, ele some para todos que o viam, e o servidor continua funcionando para os outros.
- CA-08: Uma mensagem malformada, maior que o limite ou enviada antes do login faz o servidor fechar só aquela conexão; os outros bots continuam jogando.
- CA-09: A simulação é determinística: a mesma sequência de comandos, com o mesmo relógio, produz exatamente a mesma sequência de eventos, em duas execuções.
- CA-10: De ponta a ponta, pela rede de verdade: o servidor de demonstração e três bots rodam o cenário do roadmap num teste automático, e o teste passa no Windows e no Linux (CI).
- CA-11: Verificação manual: o dono roda o servidor e o `demo` do bot em dois terminais e vê, no texto do bot, que os dois bots perto se viram e que o terceiro só os viu ao chegar perto.

## Como o dono testa

**1. Rodar todos os testes.** No terminal, na pasta do projeto:

```bash
dotnet test --solution Elysium.slnx
```

No fim deve aparecer que todos os testes passaram, sem nenhuma falha.

**2. Ver o servidor e os bots funcionando.** Abra **dois** terminais na pasta do projeto.

No primeiro, ligue o servidor e deixe-o aberto:

```bash
dotnet run --project samples/Elysium.Demo.Server
```

Ele mostra uma linha dizendo que está esperando conexões.

No segundo, rode o cenário dos três bots:

```bash
dotnet run --project tools/Elysium.Bot -- demo
```

O texto aparece em inglês, porque é texto do programa. O que conferir:

- os bots `Ana` e `Bruno` entram perto um do outro, e cada um diz que viu o outro (`sees`);
- o bot `Caio` entra longe e diz que não vê ninguém;
- `Caio` anda até perto, e então ele vê `Ana` e `Bruno`, e eles veem `Caio`;
- `Caio` se afasta, e `Ana` e `Bruno` somem para ele (`lost sight of`);
- a última linha diz que o cenário passou (`PASSED`).

Para desligar o servidor, aperte `Ctrl+C` no primeiro terminal.
