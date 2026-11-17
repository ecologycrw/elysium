---
name: consolidar
description: Faxina periódica da documentação do Elysium - contradições, documentos desatualizados, regras duplicadas, propostas e itens provisórios antigos. Use a cada cerca de 5 fatias ou quando o dono pedir.
---

# /consolidar

## Pré-condições

- Nenhuma fatia em andamento e a árvore de trabalho limpa.

## Passos

1. **Branch.** `git switch main`, `git pull --ff-only` e `git switch -c consolidacao/AAAA-MM-DD`.
2. **Auditoria.** Chame o subagente `auditor` com a ferramenta de agentes e peça a auditoria completa da documentação.
3. **Confira os achados** no texto real. O auditor também pode errar.
4. **Apresente ao dono,** em português simples, a limpeza proposta:
   - o que apagar;
   - o que juntar;
   - o que corrigir;
   - o que transformar em trava.

   Aplique só o que ele aprovar. Mudanças em travas pedem a confirmação dele em cada edição.
5. **Verifique.** Rode `/verificar`.
6. **Commit e PR.** Commit `docs(consolidation): ...`, `git push -u origin consolidacao/AAAA-MM-DD` e PR com `gh pr create`. O dono aprova e faz o merge.
