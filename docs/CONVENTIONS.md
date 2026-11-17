# Convenções

> Aqui fica só o que as ferramentas não verificam sozinhas. Formatação, nomes, avisos, APIs proibidas e dependências entre projetos são garantidos por `.editorconfig`, analisadores, testes de arquitetura e `Elysium.Checks`.

## Idiomas

| O quê | Idioma |
|---|---|
| Código, identificadores, comentários no código, mensagens de commit | inglês |
| Specs, planos, relatórios, ADRs, STATUS e demais docs do projeto | português |
| Documentação para quem usa a engine | decidido depois (provavelmente inglês) |

## Nomes (padrão .NET)

- **Projetos:** `Elysium.<Área>[.<Subárea>]`, em PascalCase com pontos. A pasta do projeto, o `.csproj`, a DLL e o namespace raiz têm o mesmo nome.
- **Subpastas de um projeto:** PascalCase sem pontos. Cada uma vira um pedaço do namespace.
- **Arquivos:** um tipo público por arquivo, com o nome do tipo. Interfaces começam com `I`.
- **Projetos de teste:** `<Projeto>.Tests`.
- **Pastas de organização do repositório:** minúsculas (`src/`, `docs/`, `tests/`).
- **Specs:** `specs/NNNN-nome-em-kebab-case/`. **ADRs:** `docs/adr/NNNN-nome-em-kebab-case.md`.

## Commits

Formato: `tipo(escopo): descrição curta em inglês`

- `tipo`: `feat`, `fix`, `docs`, `test`, `refactor`, `perf`, `build`, `ci` ou `chore`.
- `escopo`: o número da fatia (`0000`) ou a área (`prep`, `docs`, `ci`).
- Dentro de uma fatia, a descrição começa pela tarefa: `feat(0000): T03 add grid movement validation`.
- A linha `Co-Authored-By` do Claude fica no fim da mensagem.

## Padrões da casa

- **Atores e mensagens:** o estado de um mapa só muda dentro do ator dele; o resto do sistema envia mensagens.
- **Ações do jogador são comandos** que o servidor valida antes de aplicar.
- **Referência por ID**, e não por referência direta entre objetos de sistemas diferentes.
- **Transação em toda troca de dono de item.**
- **Conteúdo é dado:** definições de itens, monstros e afins vêm de dados validados.
- **A interface pensa no destino, a implementação pensa no agora.**
- `record` para mensagens e dados imutáveis.
- `async` só nas fronteiras (rede, banco, arquivos), nunca dentro do tick.
- Container de injeção de dependência só na montagem da aplicação, nunca dentro da simulação.
- Source generators em vez de reflexão em tempo de execução.

## Antipadrões proibidos

- Singletons e "managers" globais.
- Herança profunda.
- Uma interface para cada classe "por precaução", e o estilo de sistemas corporativos em geral (MediatR, AutoMapper, repositórios genéricos para tudo, camadas sem motivo).
- Abstração antes da necessidade.
- Estado mutável compartilhado entre mapas.
- Otimização sem medição.

## APIs proibidas na simulação

Os projetos `*.Server` e `*.Shared` não podem usar relógio do sistema, `Random`, locks, `Thread.Sleep` nem console (lista em `src/BannedSymbols.txt`; INV-03 e INV-04). O build falha se alguém usar.

A única exceção é o arquivo que implementa o relógio real ou o RNG real do servidor. Ele usa `#pragma warning disable RS0030` com um comentário explicando o motivo. O revisor confere todo `#pragma` desse tipo, e nenhum outro arquivo pode ter um.

## Regra de três

Uma abstração só nasce quando o mesmo problema aparece pela terceira vez. Na primeira vez, resolve-se direto. Na segunda, nota-se a repetição. Na terceira, abstrai-se, já sabendo o que de fato se repete. A regra vale também para criar projetos novos.

## Código de referência

A IA imita o código que já existe. Por isso, os melhores exemplos de cada padrão são listados aqui conforme surgem (por exemplo: "para criar um novo tipo de mensagem, siga o arquivo X").

*(Nenhum ainda.)*

## Testes

- O framework é o xUnit v3. O nome do teste descreve o comportamento: `Move_rejects_step_faster_than_speed`.
- Um teste que prova um critério de aceitação recebe `[Trait("CA", "NNNN-CA-xx")]`.
- Sorteios e tempo nos testes vêm do RNG e do relógio injetados, nunca do sistema.
- Um teste nunca é apagado ou enfraquecido para "fazer passar".

## Documentação

- Curta. Aponta para outro documento em vez de repetir o que está nele.
- Diagramas Mermaid quando ajudarem:

| Tipo | Para quê |
|---|---|
| `flowchart LR` | fluxos e processos |
| `flowchart TB` | camadas e hierarquias |
| `sequenceDiagram` | quem fala com quem, e em que ordem |
| `stateDiagram-v2` | estados e transições |
| `erDiagram` | tabelas do banco |

- No máximo cerca de 15 elementos por diagrama, uma ideia por diagrama.
- O diagrama complementa o texto: toda regra importante também fica escrita.
- O que puder ser gerado a partir do código é gerado, não desenhado à mão.

## Scripts

- Hooks do Git em `sh` (rodam no Windows, pelo Git, e no Linux).
- Hooks do Claude Code em PowerShell 5.1, salvos em UTF-8 com BOM.

## Conteúdo de terceiros

- Nada de nomes, assets, textos, arquivos ou protocolos de outros jogos. Mapas, itens, monstros, eventos e textos são originais.
- Assets externos só com licença compatível (CC0, CC-BY com atribuição, MIT), registrada junto do asset.
- Código de outros projetos só com licença compatível com a MIT e com atribuição.
