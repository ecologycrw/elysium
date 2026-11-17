# Propostas

Fila única de tudo o que surgiu fora da tarefa atual: ideias, problemas, regras que estão faltando e lições aprendidas. A fila é processada na `/retrospectiva`, e cada entrada vira fatia, decisão, regra ou trava, ou é descartada. Ver [WORKFLOW.md](WORKFLOW.md#propostas-como-as-necessidades-aparecem).

Formato de cada entrada (o modelo completo fica em `templates/proposal.md`):

```markdown
### P-0001: título curto
- Origem: fatia e tarefa, feedback do dono, medição ou checagem
- Tipo: fatia | regra | trava | skill | doc | decisão | bug
- Evidência: o que mostra que isso é necessário
- Prioridade sugerida: alta | média | baixa
- Status: aberta
```

## Abertas

### P-0001: o /planejar move a fatia escolhida para "Agora" no ROADMAP
- Origem: feedback do dono, no /planejar da fatia 0
- Tipo: skill
- Evidência: nas palavras do dono, "o /planejar deve sempre mover a fatia escolhida para 'Agora' no ROADMAP. Evidência: na fatia 0 foi preciso pedir isso manualmente." O passo a passo do `/planejar` não fala do ROADMAP, e a skill proíbe mudá-lo sem aprovação. Na fatia 0, o ROADMAP ainda mostrava a preparação em "Agora" até o dono pedir a mudança. A aprovação da spec poderia valer também para essa atualização.
- Prioridade sugerida: média
- Status: aberta

### P-0002: régua de qualidade sênior
- Origem: feedback do dono, no fim da implementação da fatia 0
- Tipo: regra e trava
- Evidência: o dono não lê código e quer garantir o padrão sênior antes que a fatia 1 copie os padrões da fatia 0. O pedido dele tem três partes:
  1. Uma seção curta "Régua de qualidade" no `docs/CONVENTIONS.md`, com o que um revisor sênior recusaria no Elysium: funções longas, classes com mais de uma responsabilidade, código repetido, tratamento de erro que esconde o problema, nomes vagos (`Manager`, `Helper`, `Data`) e testes que verificam a implementação em vez do comportamento.
  2. Esses itens na lista obrigatória do subagente `revisor`.
  3. Os analisadores de complexidade do .NET ligados (CA1502, CA1505, CA1506), com limites calibrados no código da fatia 0.

  As partes 2 e 3 mexem em travas (`.claude/agents/` e a configuração de análise) e precisam da aprovação do dono. Como a prioridade é alta e o objetivo é chegar antes da fatia 1, a retrospectiva da fatia 0 pode decidir aplicá-la ainda neste PR.
- Prioridade sugerida: alta
- Status: aberta

## Processadas

*(nenhuma)*
