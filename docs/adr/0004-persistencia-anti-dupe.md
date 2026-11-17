---
id: 0004
titulo: Persistência à prova de dupe
status: aceita
data: 2026-11-16
substitui:
substituida-por:
---

# ADR 0004: Persistência à prova de dupe

## Contexto

Os itens são a economia do jogo, e um dupe destrói um servidor. Um dos dupes mais clássicos de MMO é este:

1. O jogador A joga um item no chão e o jogador B pega.
2. B é salvo, e o servidor cai antes de A ser salvo.
3. A volta do snapshot antigo ainda com o item, e agora existem dois.

Se o personagem for salvo como um bloco serializado único (o jeito mais fácil, e o que uma IA tende a gerar sem instrução), nenhuma restrição do banco impede isso.

## Decisão

1. **Todo item é uma linha própria** no banco, com ID global único e **um único dono** (personagem, chão, baú, loja, correio ou sistema), garantido por restrição no banco.
2. **Toda troca de dono que envolve outro jogador**, ou algo que outro jogador pode acessar (trade, item jogado no chão, baú compartilhado, loja pessoal, correio, leilão), é **uma transação no banco**, confirmada antes de o resultado aparecer para os jogadores.
3. Mudanças internas (mover um item no próprio inventário, pegar o drop de um monstro) ficam em memória e entram no snapshot.
4. **O snapshot de um mapa grava todos os personagens do mapa no mesmo tick, numa única transação.** O ator single-threaded (ADR 0003) torna isso simples.
5. **Ledger que só aceita novos registros** para toda criação, transferência e destruição de item. Ele permite investigar dupes, reverter fraudes e medir a economia.
6. **Lease de posse** na troca de mapa: o personagem existe em um só ator por vez, e mensagens para o dono antigo são rejeitadas.
7. **PostgreSQL** é a fonte da verdade a partir da fatia 2. Antes disso, uma implementação em memória fica atrás da mesma interface. Um cache nunca é a fonte da verdade da economia.

## Consequências

- Se o servidor cair, os personagens voltam ao último snapshot, mas o rollback não consegue duplicar nada.
- Cada operação de troca precisa de testes que derrubam o servidor e desconectam jogadores no meio dela.
- As trocas entre jogadores custam uma ida ao banco. Elas são raras perto do resto da simulação.

## Alternativas consideradas

- **Personagem salvo como um bloco único:** vulnerável ao dupe descrito acima.
- **Gravar toda mudança no banco na hora:** lento e desnecessário para as mudanças internas.
