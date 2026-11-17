---
id: 0001
titulo: C# e .NET 10 em tudo
status: aceita
data: 2026-11-16
substitui:
substituida-por:
---

# ADR 0001: C# e .NET 10 em tudo

## Contexto

Quase todo o código é escrito por IA, e o dono do produto verifica o trabalho pelo comportamento (testes, bots, jogando), não lendo o código linha por linha. O servidor é o produto principal, e cliente e servidor precisam compartilhar uma biblioteca de regras. O dono lê um pouco de C++ e de C#.

## Decisão

Usar **C# com .NET 10** (versão LTS) no servidor, no cliente, nas regras compartilhadas, nas ferramentas e na lógica de jogo escrita pelos desenvolvedores. Uma linguagem, um build (`dotnet`), um depurador.

## Consequências

- Um erro gera uma exceção que aponta a linha exata, o que facilita o diagnóstico pela IA.
- O build é simples, sem a complexidade de CMake e vcpkg.
- O .NET é forte em servidores.
- Muitos desenvolvedores indie já conhecem C#. A lógica de jogo também é escrita em C#, o que dispensa uma linguagem de script como Luau.
- Algumas bibliotecas de jogo só existem em C++ (por exemplo, o RmlUi). A saída é usar bindings nativos ou fazer uma implementação própria.
- O coletor de lixo (GC) exige disciplina no cliente: o loop do frame não pode alocar memória, e isso é medido em teste.

## Alternativas consideradas

- **C++:** tem o maior ecossistema de jogos, mas comportamento indefinido, corrupção de memória e build complexo amplificam os erros da IA.
- **Rust:** o compilador barra muitos erros, mas o dono não lê a linguagem.
- **C# no servidor e C++ no cliente:** quebraria a biblioteca de regras compartilhada.
