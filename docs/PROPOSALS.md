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

*(nenhuma)*

## Processadas

### P-0001: o /planejar move a fatia escolhida para "Agora" no ROADMAP
- Origem: feedback do dono, no /planejar da fatia 0
- Tipo: skill
- Evidência: nas palavras do dono, "o /planejar deve sempre mover a fatia escolhida para 'Agora' no ROADMAP. Evidência: na fatia 0 foi preciso pedir isso manualmente." O passo a passo do `/planejar` não fala do ROADMAP, e a skill proíbe mudá-lo sem aprovação. Na fatia 0, o ROADMAP ainda mostrava a preparação em "Agora" até o dono pedir a mudança. A aprovação da spec poderia valer também para essa atualização.
- Prioridade sugerida: média
- Status: aceita
- Destino: skill. Aplicada na retrospectiva da fatia 0: o `/planejar` move a fatia para "Agora" quando o dono aprova a spec.

### P-0002: régua de qualidade sênior
- Origem: feedback do dono, no fim da implementação da fatia 0
- Tipo: regra e trava
- Evidência: o dono não lê código e quer garantir o padrão sênior antes que a fatia 1 copie os padrões da fatia 0. O pedido dele tem três partes:
  1. Uma seção curta "Régua de qualidade" no `docs/CONVENTIONS.md`, com o que um revisor sênior recusaria no Elysium: funções longas, classes com mais de uma responsabilidade, código repetido, tratamento de erro que esconde o problema, nomes vagos (`Manager`, `Helper`, `Data`) e testes que verificam a implementação em vez do comportamento.
  2. Esses itens na lista obrigatória do subagente `revisor`.
  3. Os analisadores de complexidade do .NET ligados (CA1502, CA1505, CA1506), com limites calibrados no código da fatia 0.

  As partes 2 e 3 mexem em travas (`.claude/agents/` e a configuração de análise) e precisam da aprovação do dono. Como a prioridade é alta e o objetivo é chegar antes da fatia 1, a retrospectiva da fatia 0 pode decidir aplicá-la ainda neste PR.
- Prioridade sugerida: alta
- Status: aceita
- Destino: regra e trava. Aplicada na retrospectiva da fatia 0: seção "Régua de qualidade" no `docs/CONVENTIONS.md`, item 7 na lista obrigatória do `revisor` e CA1502, CA1505 e CA1506 ligados no `.editorconfig` com os limites padrão do .NET, por decisão do dono. Nenhum código da fatia 0 passou dos limites, então nada precisou ser afrouxado.

### P-0003: testes de mutação com Stryker.NET
- Origem: feedback do dono, no fim da implementação da fatia 0
- Tipo: trava
- Evidência: nas palavras do dono, "o risco de testes que passam sem verificar nada não é pego pelo build nem pela CI". O pedido é usar testes de mutação para provar que os testes detectam erros de verdade, começando pelos projetos de servidor (`Elysium.Core.Server` e `Elysium.Classic.Server`). O [ADR 0009](adr/0009-ferramentas-de-verificacao.md) já prevê o Stryker.NET "mais adiante". Ele é uma ferramenta externa nova, e rodá-lo na CI muda uma trava (`.github/`). As duas coisas precisam da aprovação do dono. Também falta decidir a nota mínima de mutação e se ela bloqueia o PR ou só aparece no relatório.
- Prioridade sugerida: média
- Status: aceita
- Destino: fatia 1, como tarefa de trava. O dono aprova a ferramenta e a nota mínima junto com a spec da fatia 1.

### P-0004: os testes de arquitetura quebram com uma dependência sem namespace
- Origem: fatia 1, T04
- Tipo: trava, bug
- Evidência: o `ServerLink` usava `Task.WhenAll(a, b)`. No .NET 10, essa chamada usa a versão com `params ReadOnlySpan<Task>`, e o compilador gera ajudantes internos (`InlineArrayElementRef` e `InlineArrayAsReadOnlySpan`, em `<PrivateImplementationDetails>`) cujos tipos genéricos não têm namespace. O `EngineModel.ForbiddenDependencies` lê `dependency.Target.Namespace.FullName` sem conferir se o namespace existe (`tests/Elysium.Architecture.Tests/EngineModel.cs`, linha 47), e dois testes de camadas falham com `NullReferenceException`, sem dizer o motivo. Foi contornado no `ServerLink` com duas esperas simples, mas qualquer código novo com a mesma construção quebra o teste de novo, e o erro não ajuda a achar a causa. Sugestão: tratar um alvo sem namespace como fora das camadas do Elysium (ele nunca é uma dependência proibida) e criar um teste que prove isso.
- Prioridade sugerida: média
- Status: aceita
- Destino: trava. Aprovada pelo dono em 2026-11-17 para ser corrigida na própria fatia 1, como a tarefa T04b, antes da T05. Decisão do dono: o teste de arquitetura ignora dependências sem namespace, porque elas nunca são tipos do Elysium; se a leitura de um tipo falhar por outro motivo, o erro diz qual tipo causou a falha; um teste prova as duas coisas (`EngineModelTests`, com o código de exemplo `Fixtures/InlineArrayFixture.cs`). O `ServerLink` voltou para o `Task.WhenAll`, que é mais claro.
