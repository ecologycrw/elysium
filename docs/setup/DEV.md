# Ambiente de desenvolvimento

## Requisitos

- Windows 10/11 64 bits. Linux também serve para build e testes.
- .NET SDK na versão do [`global.json`](../../global.json): 10.0.400 ou mais novo da mesma linha.
- Git.
- GitHub CLI (`gh`), para abrir e acompanhar PRs.
- Node.js LTS: opcional, só para validar os diagramas Mermaid localmente (a CI sempre valida).

## Primeira vez num clone

Ative os hooks do Git, uma vez por clone:

```bash
git config core.hooksPath .githooks
```

A identidade dos commits da IA e a assinatura dos commits são configuradas só neste repositório, na parte 4 da preparação.

## Comandos do dia a dia

| O quê | Comando |
|---|---|
| Build | `dotnet build Elysium.slnx` |
| Testes | `dotnet test --solution Elysium.slnx` |
| Checagens de docs e estrutura | `dotnet run --project tools/Elysium.Checks -- all` |
| Conferir a formatação | `dotnet format whitespace Elysium.slnx --verify-no-changes` |
| Corrigir a formatação | `dotnet format whitespace Elysium.slnx` |

Os testes rodam na Microsoft Testing Platform, ativada no `global.json`; por isso o `dotnet test` usa `--solution`.

## O que roda automaticamente

| Quando | O quê | Onde |
|---|---|---|
| Antes de cada commit | busca de segredos; com código: formatação, build, testes e checagens; só com docs: checagem de docs | `.githooks/pre-commit` |
| Ao escrever a mensagem de commit | formato `tipo(escopo): descrição` | `.githooks/commit-msg` |
| Antes de cada push | bloqueia envio para `main` e `master` | `.githooks/pre-push` |
| A cada PR e a cada push na `main` | tudo de novo, em Ubuntu e Windows, mais a validação dos diagramas e o resumo de segurança do PR | `.github/workflows/ci.yml` |

## Se um hook bloquear

Leia a mensagem: ela diz o que corrigir. **Nunca** use `--no-verify` para pular uma checagem. Se a regra parecer errada, registre uma proposta (ver [WORKFLOW.md](../WORKFLOW.md#propostas-como-as-necessidades-aparecem)).

## APIs proibidas na simulação

Projetos `*.Server` e `*.Shared` não podem usar as APIs listadas em [`src/BannedSymbols.txt`](../../src/BannedSymbols.txt): relógio do sistema, `Random`, locks, `Thread.Sleep` e console. O build falha com o erro `RS0030`, que explica o que usar no lugar. Ver [CONVENTIONS.md](../CONVENTIONS.md#apis-proibidas-na-simulação).
