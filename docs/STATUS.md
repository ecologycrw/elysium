---
fase: fatia
spec-atual: 0000
branch: fatia/0000-servidor-e-bots
atualizado-em: 2026-11-17
---

# Status

## Agora

**Fatia 0 (Servidor e bots): fechada, aguardando o teste do dono.** PR aberto. O que testar está no [report](../specs/0000-servidor-e-bots/report.md#o-que-você-deve-testar).

## Próxima ação

O dono testa e conta o que achou. Depois, `/retrospectiva` no mesmo PR, antes da aprovação e do merge.

## Bloqueios

Nenhum. Decisão pendente do dono: o [ADR 0011](adr/0011-versao-do-protocolo-no-login.md) (versão do protocolo no login), com status `proposta`.

## Histórico

| Data | O que aconteceu |
|---|---|
| 2026-11-16 | Plano de preparação aprovado; parte 1 escrita |
| 2026-11-17 | Parte 1 aprovada e commitada; conta de máquina `elysium-ci-dev` configurada como identidade dos commits |
| 2026-11-17 | Parte 2 concluída: 5 projetos, testes de arquitetura, `Elysium.Checks`, hooks do Git e CI |
| 2026-11-17 | Parte 3 concluída: ambiente de IA; retrospectiva passa a rodar no mesmo PR da fatia, antes do merge |
| 2026-11-17 | Parte 4: guia do GitHub e regra de proteção da `main` escritos; PR #1 aberto |
| 2026-11-17 | PR #1 bloqueado por commits sem assinatura (feitos antes da chave); commits copiados assinados para `preparacao/assinada` e PR #2 aberto no lugar |
| 2026-11-17 | PR #2 mergeado: preparação concluída |
| 2026-11-17 | Skill `/status` renomeada para `/situacao`, porque o nome conflitava com o comando interno do Claude Code |
| 2026-11-17 | Fatia 0 planejada; o dono aprovou a spec, a biblioteca `Microsoft.Extensions.Logging`, as mudanças em `Directory.Packages.props` e em `Elysium.Architecture.Tests`, o protocolo v1 e os destinos de TLS e login (fatia 2). ROADMAP atualizado: fatia 0 em "Agora". Proposta P-0001 registrada |
| 2026-11-17 | Fatia 0 implementada (T01 a T12); propostas P-0002 e P-0003 registradas a pedido do dono |
| 2026-11-17 | Revisão independente da fatia 0: 4 achados importantes e os menores corrigidos; ADR 0011 proposto |
| 2026-11-17 | Fatia 0 fechada: report escrito e PR aberto para o teste do dono |
