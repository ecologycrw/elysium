# Ambiente de desenvolvimento

## Requisitos

- Windows 10/11 64 bits. Linux também serve para build e testes.
- .NET SDK na versão do [`global.json`](../../global.json): 10.0.400 ou mais novo da mesma linha.
- Git.
- GitHub CLI (`gh`), para abrir e acompanhar PRs.
- Node.js LTS: opcional, só para validar os diagramas Mermaid localmente (a CI sempre valida).
- Windows SDK e Vulkan SDK: só para recompilar os shaders do cliente (`sh src/core/Elysium.Core.Client.Sdl/Shaders/compile.sh`). Os compilados ficam no repositório, então o build não precisa deles (TD-0010).

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
| Servidor de demonstração | `dotnet run --project samples/Elysium.Demo.Server` |
| Cliente de demonstração (com o servidor ligado) | `dotnet run --project samples/Elysium.Demo.Client -- --name Ana` |

Os testes rodam na Microsoft Testing Platform, ativada no `global.json`; por isso o `dotnet test` usa `--solution`.

## O que roda automaticamente

| Quando | O quê | Onde |
|---|---|---|
| Antes de cada commit | busca de segredos; com código: formatação, build, testes e checagens; só com docs: checagem de docs | `.githooks/pre-commit` |
| Ao escrever a mensagem de commit | formato `tipo(escopo): descrição` | `.githooks/commit-msg` |
| Antes de cada push | bloqueia envio para `main` e `master` | `.githooks/pre-push` |
| A cada PR e a cada push na `main` | tudo de novo, em Ubuntu e Windows, mais a validação dos diagramas, os testes de mutação e o resumo de segurança do PR | `.github/workflows/ci.yml` |

## Testes de mutação

O Stryker.NET (ferramenta local, versão fixa em `dotnet-tools.json`) muda pedacinhos do código de `Elysium.Core.Server` e `Elysium.Classic.Server` e confere se algum teste falha. A nota é a porcentagem de mudanças que os testes pegaram. Abaixo de **80%**, o comando termina com erro e o job `mutation` da CI fica vermelho (aprovado pelo dono em 2026-11-17, proposta P-0003).

Para rodar na sua máquina (cerca de 2 minutos por projeto):

```bash
dotnet tool restore
cd tests/Elysium.Core.Server.Tests
dotnet stryker
```

O relatório fica em `StrykerOutput/` (ignorado pelo Git). A configuração de cada projeto está no `stryker-config.json` da pasta de testes dele. São ignoradas só as mudanças que nenhum teste de comportamento consegue pegar: `ConfigureAwait`, `ArgumentNullException.ThrowIfNull` e as chamadas de log. Mudar essa lista ou a nota mínima precisa da aprovação do dono.

**Modo preview.** O Stryker roda com `"test-runner": "mtp"`, o modo que entende o Microsoft Testing Platform do xUnit v3. Esse modo ainda está marcado como preview pelo Stryker (TD-0009 em [TECH_DEBT.md](../TECH_DEBT.md)). O modo padrão (`vstest`) não encontra os nossos testes.

### Se o job `mutation` falhar

1. **Leia a última linha do job.** Ela diz qual dos dois casos é.
2. **"The mutation score is lower than the configured break threshold":** a falha é de verdade. Um código novo ficou sem teste, ou um teste deixou de conferir alguma coisa. O resumo do job mostra a nota por arquivo; rode o Stryker localmente e abra o relatório HTML para ver as mudanças que sobreviveram. Escreva testes que verificam o comportamento que importa (não testes só para "matar mutantes"). Nunca baixe a nota mínima nem amplie a lista de ignorados sem a aprovação do dono.
3. **Qualquer outro erro do Stryker** (os testes não foram encontrados, o executor travou, um erro interno): provavelmente é o modo preview. Rode o job de novo uma vez (**Re-run jobs**, na página do check). Se falhar de novo:
   - a IA registra uma proposta com o texto do erro, para, e avisa o dono;
   - a IA não mexe no job, na nota nem na versão do Stryker sem aprovação (o workflow é uma trava);
   - se o PR não puder esperar, só o dono pode decidir tirar o `mutation` dos checks obrigatórios da `main` por um tempo (ver [GITHUB.md](GITHUB.md#10-tornar-o-check-de-mutação-obrigatório-dono-navegador)), e isso fica registrado no STATUS até ele voltar.

## Se um hook bloquear

Leia a mensagem: ela diz o que corrigir. **Nunca** use `--no-verify` para pular uma checagem. Se a regra parecer errada, registre uma proposta (ver [WORKFLOW.md](../WORKFLOW.md#propostas-como-as-necessidades-aparecem)).

## APIs proibidas na simulação

Projetos `*.Server` e `*.Shared` não podem usar as APIs listadas em [`src/BannedSymbols.txt`](../../src/BannedSymbols.txt): relógio do sistema, `Random`, locks, `Thread.Sleep` e console. O build falha com o erro `RS0030`, que explica o que usar no lugar. Ver [CONVENTIONS.md](../CONVENTIONS.md#apis-proibidas-na-simulação).
