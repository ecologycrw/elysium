---
id: 0003
titulo: Servidor autoritativo com um ator por mapa
status: aceita
data: 2026-11-16
substitui:
substituida-por:
---

# ADR 0003: Servidor autoritativo com um ator por mapa

## Contexto

Num MMORPG, tudo o que roda no PC do jogador pode ser alterado: memória, pacotes, arquivos. Jogos do gênero sofreram muito com speed hack, dupe e bots. Além disso, código concorrente com estado compartilhado é uma das maiores fontes de bugs difíceis, principalmente em código gerado por IA.

## Decisão

- **Autoridade total do servidor.** O cliente envia intenções ("andar até ali", "usar a skill X no alvo Y"); o servidor valida, decide e transmite o resultado.
- **Um ator single-threaded por mapa ou instância de evento**, com o próprio loop de tick e a própria fila de mensagens. Dentro do mapa não há locks.
- O paralelismo vem de rodar muitos mapas ao mesmo tempo. Mapas conversam só por mensagens.
- **Simulação determinística:** o tempo vem do relógio do tick e os sorteios do RNG do servidor, os dois injetáveis nos testes.
- Tick configurável entre 10 e 20 por segundo.
- No início, **um processo só**, com os serviços (login, social, agendador) como módulos.

## Consequências

- Não existe data race dentro de um mapa, o que é ideal para desenvolvimento com IA.
- Os testes são reproduzíveis.
- Um snapshot consistente do mapa inteiro sai sem custo extra (ver ADR 0004).
- Speed hack não funciona, porque o movimento é validado pelo tempo no servidor.
- Limite: um mapa muito lotado depende de um núcleo da CPU. Se uma medição mostrar isso, o cálculo de AOI vai para jobs paralelos.

## Alternativas consideradas

- **Paralelizar a lógica dentro do mapa:** locks e bugs de concorrência, sem ganho real com algumas centenas de entidades.
- **ECS obrigatório no servidor:** ganho pequeno nessa escala. Pode ser usado para organizar dados, se ajudar.
- **Microsserviços desde o início:** complexidade de quem já tem dezenas de milhares de jogadores.
