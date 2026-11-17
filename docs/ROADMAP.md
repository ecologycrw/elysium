# Roadmap

> Muda só com aprovação do dono do produto. A retrospectiva decide a próxima fatia; ela passa de "Próximas" para "Agora" quando o dono aprova a spec dela, no `/planejar`.

## Concluídas

- **Preparação** (partes 1 a 4): documentos, esqueleto do código com as garantias automáticas, ambiente de IA e GitHub.
- **Fatia 0: Servidor e bots.** Servidor por ticks com login, movimento autoritativo na grade e área de interesse; bot headless com o cenário dos três bots. Ver [o report](../specs/0000-servidor-e-bots/report.md).

## Agora

Nenhuma fatia em andamento. A próxima é a **fatia 1**, decidida na retrospectiva da fatia 0. Ver o [STATUS.md](STATUS.md).

## Próximas

| Fatia | Nome | O que entrega, do ponto de vista de quem testa |
|---|---|---|
| 1 | Dois jogadores se veem | Janela, terreno, câmera 3/4, personagem simples e clicar para andar; dois clientes no mesmo mapa. Inclui os testes de mutação nos projetos de servidor (proposta P-0003) |
| 2 | Caçar | Monstros com spawn e IA simples, combate autoritativo, EXP, drops no chão, inventário em grade e persistência com ledger |

## Depois

- **Economia:** trade, loja de NPC, baú, upgrade e combinação de itens, tudo transacional, com testes de dupe automáticos (desconectar e derrubar o servidor no meio de cada operação).
- **Social e eventos:** party, guild, chat, agendador de eventos e a primeira instância de evento.
- **Escala:** bots de carga, o cenário de cerco, qualidade reduzida para personagens e efeitos distantes, otimização de banda.
- **Ferramentas:** editor de mapas, editores de dados, editor de efeitos, cluster local com um clique.
- **Distribuição:** patcher, pacotes por hash e patches que baixam só a diferença.
- **Plataformas:** Linux e mobile.

Quando uma fatia de gameplay depende de algo que ainda não existe (por exemplo, um personagem animado), uma fatia de fundação técnica entra antes dela.
