@AGENTS.md

# Claude Code no Elysium

O `AGENTS.md`, importado acima, contém as regras. Aqui fica só o que é específico do Claude Code.

## Skills

Os comandos do ciclo ficam em `.claude/skills/`: `/status`, `/planejar`, `/implementar`, `/verificar`, `/revisar`, `/fechar`, `/retrospectiva`, `/consolidar` e `/propor`. Quando o dono pedir algo que corresponde a uma etapa, use a skill dessa etapa.

## Subagentes

- `revisor`: usado pelo `/revisar`. Trabalha com contexto limpo e só lê.
- `auditor`: usado pelo `/consolidar`.
- Para buscas amplas ou leitura de muitos arquivos, use subagentes de exploração com um modelo mais barato. Guarde o modelo principal para as decisões e o código difícil.

## Hooks e permissões

- **No início da sessão:** um hook mostra a branch, os últimos commits, o STATUS e as propostas abertas.
- **Antes de cada comando de terminal:** o `guard-bash.ps1` bloqueia comandos proibidos e explica o motivo.
- **Ao terminar um turno com alterações não commitadas:** o `stop-check.ps1` roda a checagem de docs e o build. Se falhar, corrija antes de encerrar.
- **Editar uma trava** pede a confirmação do dono. Explique antes por que a mudança é necessária.

## Commits e PRs

- A identidade da conta de máquina (`elysium-ci-dev`) já está configurada neste repositório. Não mude o `git config`.
- Termine cada mensagem de commit com a linha `Co-Authored-By` indicada pelo Claude Code.
- A descrição do PR é em português e termina com a linha de atribuição do Claude Code.
