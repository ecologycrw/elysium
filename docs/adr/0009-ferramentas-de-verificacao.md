---
id: 0009
titulo: Ferramentas de verificação
status: aceita
data: 2026-11-16
substitui:
substituida-por:
---

# ADR 0009: Ferramentas de verificação

## Contexto

O dono do produto não revisa código linha por linha. As regras precisam ser garantidas por máquina, não depender de a IA lembrar delas. Princípio: **toda regra que uma máquina consegue verificar vira teste, analisador, hook ou CI, não parágrafo.**

## Decisão

| Necessidade | Ferramenta |
|---|---|
| Testes | xUnit v3 |
| Dependências entre camadas e nomes | testes de arquitetura com ArchUnitNET |
| APIs proibidas na simulação | `Microsoft.CodeAnalysis.BannedApiAnalyzers` com `src/BannedSymbols.txt` |
| Estilo e qualidade | `.editorconfig`, `dotnet format`, avisos tratados como erros, nullable ativado, `AnalysisLevel latest-recommended` |
| Compatibilidade com NativeAOT | `IsAotCompatible` nas bibliotecas |
| Docs, specs e estrutura do repositório | `tools/Elysium.Checks`, em C#, com testes próprios |
| Antes de cada commit e push | hooks do Git em `sh` |
| Antes de cada comando da IA, no início e no fim da sessão | hooks do Claude Code em PowerShell 5.1 |
| Verificação independente | GitHub Actions em Ubuntu e Windows |
| Diagramas | `@mermaid-js/mermaid-cli` na CI |
| Força dos testes (mais adiante) | testes de mutação com Stryker.NET |

Os hooks do Claude Code usam PowerShell 5.1 porque ele vem com o Windows e não depende de build. O pwsh 7 e o jq não estão instalados na máquina de desenvolvimento.

## Consequências

- Uma regra quebrada aparece como build vermelho, teste falhando ou commit bloqueado, com a explicação.
- Há duas linguagens de script além do C# (sh e PowerShell), ambas pequenas e restritas aos hooks.
- O pre-commit roda build e testes, o que deixa cada commit mais lento. Isso é reavaliado se ficar incômodo.

## Alternativas consideradas

- **Regras só em texto:** a IA pode esquecer ou ignorar.
- **Hooks do Claude Code em C#:** dependeriam de um build já feito, e um hook que não roda deixaria a ação passar.
- **NetArchTest no lugar do ArchUnitNET:** fica como alternativa se houver incompatibilidade.
