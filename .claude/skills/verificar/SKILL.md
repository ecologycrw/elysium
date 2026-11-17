---
name: verificar
description: Roda todas as verificações do Elysium (formatação, build, testes e checagens) e mostra a saída real, com cada critério de aceitação ligado à sua prova. Use depois de cada tarefa, antes de revisar e antes de fechar uma fatia.
---

# /verificar

## Passos

Rode, nesta ordem, e mostre a saída real (resumida, mas com os números):

```bash
dotnet format whitespace Elysium.slnx --verify-no-changes
dotnet build Elysium.slnx --nologo -v q
dotnet test --solution Elysium.slnx --no-build
dotnet run --project tools/Elysium.Checks --no-build -- all
```

Se houver uma fatia em andamento, monte também a tabela de critérios:

| CA | Prova | Situação |
|---|---|---|
| CA-01 | teste com `[Trait("CA", "NNNN-CA-01")]` encontrado em `tests/...` | passou |
| CA-02 | verificação manual prevista no plano | pendente |

Para achar as provas, procure `"NNNN-CA-` nos arquivos `.cs` de `tests/`.

## Resultado

- **Verde** só se todos os comandos passaram nesta execução.
- Se algo falhar, mostre o erro, explique em linguagem simples e corrija (dentro do escopo da tarefa).
- Nunca diga que passou sem ter rodado.
