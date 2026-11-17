---
name: planejar
description: Escolhe a próxima fatia do Elysium e escreve a spec e o plano para o dono aprovar. Use quando não houver fatia em andamento e o dono quiser começar a próxima etapa.
---

# /planejar

Transforma o próximo item do roadmap numa spec e num plano aprovados. **Não implementa nada.**

## Pré-condições

- Nenhuma fatia em andamento: o `spec-atual` do `docs/STATUS.md` está vazio. Se não estiver, pare e explique ao dono.
- A árvore de trabalho está limpa (`git status --short` vazio).

## Passos

1. **Contexto.** Leia `docs/ROADMAP.md`, `docs/STATUS.md`, as propostas abertas em `docs/PROPOSALS.md`, `docs/TECH_DEBT.md`, o report da última fatia e as seções de `docs/ARCHITECTURE.md` que a fatia toca.
2. **Escolha da fatia.** É a primeira de "Próximas" no ROADMAP, a menos que a última retrospectiva tenha decidido outra. Se ela depender de algo que ainda não existe, proponha antes uma fatia de fundação e pergunte ao dono.
3. **Branch.** Rode `git switch main`, `git pull --ff-only` e `git switch -c fatia/NNNN-nome`. A fatia N usa o número N com quatro dígitos; o nome é curto, em kebab-case.
4. **Spec.** Crie `specs/NNNN-nome/spec.md` a partir de `templates/spec.md`, com `status: rascunho`. Os critérios de aceitação são verificáveis: cada um é provado por um teste automático ou por uma verificação manual descrita. Escreva "Como o dono testa" com passos que ele consiga seguir sozinho.
5. **Plano.** Crie `plan.md` a partir de `templates/plan.md`. Preencha com cuidado a "Visão de destino" e as "Simplificações provisórias" (ver "interface pensa no destino, implementação no agora" em `docs/CONVENTIONS.md`). Liste bibliotecas novas, ADRs necessários e mudanças de banco ou protocolo.
6. **Checagem.** Rode `dotnet run --project tools/Elysium.Checks -- docs`.
7. **Apresente ao dono**, em português simples:
   - o que a fatia entrega e como ele vai testar;
   - os critérios de aceitação em linguagem simples;
   - o que fica de fora;
   - o que precisa da decisão dele (bibliotecas novas, ADRs, banco, protocolo).

   Pergunte se ele aprova.
8. **Só depois de um "sim" explícito:**
   - mude o status da spec para `aprovada` e preencha `aprovada-em`;
   - atualize o STATUS (`spec-atual: NNNN`, `branch`, "Agora" e "Próxima ação");
   - commit `docs(NNNN): plan slice N` e `git push -u origin fatia/NNNN-nome`.

Se o dono pedir mudanças, ajuste a spec e o plano e volte ao passo 7.

## Nunca

- Começar a implementar.
- Mudar o ROADMAP ou a VISION sem a aprovação do dono.
