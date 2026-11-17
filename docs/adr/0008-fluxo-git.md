---
id: 0008
titulo: Fluxo Git com conta de máquina e merge só pelo dono
status: aceita
data: 2026-11-16
substitui:
substituida-por:
---

# ADR 0008: Fluxo Git com conta de máquina e merge só pelo dono

## Contexto

A IA precisa salvar e enviar o trabalho sozinha, mas a versão oficial só pode mudar com a aprovação do dono. O GitHub não deixa ninguém aprovar o próprio PR. Se a IA usar a conta do dono, ela tem os mesmos poderes dele, inclusive para fazer merge e desligar as proteções.

## Decisão

- **Conta de máquina** só para a IA, com permissão *Write* no repositório. Ela faz os commits, envia as branches e abre os PRs. O dono aprova e faz o merge com a própria conta, pelo navegador.
- Na máquina de desenvolvimento, o `gh` e o Git ficam logados **só na conta da IA**, com um token que vale só para este repositório.
- A identidade dos commits (nome e e-mail noreply da conta da IA) é configurada **só neste repositório**. Os commits trazem a linha `Co-Authored-By` do Claude.
- Os commits são **assinados com uma chave SSH** da conta da IA.
- **Uma branch por fatia** (`fatia/NNNN-nome`). Um commit por tarefa, se o build e os testes passarem. A branch é enviada automaticamente.
- O `/fechar` abre o PR. O dono aprova e faz o **squash merge**, que deixa um commit por fatia na `main`.
- A `main` é protegida por um ruleset do GitHub:
  - exige PR com 1 aprovação e revisão do code owner;
  - exige a CI verde em Ubuntu e Windows;
  - bloqueia force push e exclusão;
  - exige commits assinados;
  - não tem exceções.
- Proibido para a IA: force push, push para a `main`, `--no-verify`, `git config`, `gh pr merge`, `gh auth` e `gh api`. Isso vale nas permissões, nos hooks e no GitHub.

## Consequências

- O histórico separa claramente o trabalho da IA (conta de máquina) das decisões do dono (aprovações e merges).
- Mesmo que todas as travas locais falhem, a `main` continua protegida no GitHub.
- O dono passa a usar a conta pessoal só pelo navegador neste PC.
- O token da IA vence e precisa ser renovado periodicamente.

## Alternativas consideradas

- **Commits na conta do dono:** ele não conseguiria aprovar os PRs, e a IA teria poderes de administrador.
- **Commits com a identidade "Claude":** o e-mail não é do dono, e não há como assinar.
