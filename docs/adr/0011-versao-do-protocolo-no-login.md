---
id: 0011
titulo: Versão do protocolo na mensagem de login, não em cada envelope
status: aceita
data: 2026-11-17
substitui:
substituida-por:
---

# ADR 0011: Versão do protocolo na mensagem de login, não em cada envelope

## Contexto

O INV-11 exige que o protocolo tenha versão explícita e que uma mudança incompatível mude a versão. O plano da fatia 0 previa um envelope com tamanho, tipo **e versão** em toda mensagem. Na implementação, a versão foi para o primeiro campo da mensagem de login, e a revisão apontou que essa mudança de protocolo precisava da aprovação do dono.

## Decisão

- A versão do protocolo vai **só no `LoginRequest`**, como o primeiro campo (2 bytes). O servidor confere a versão antes de ler o resto e recusa com `LoginRejected(VersionMismatch)`.
- Duas coisas **nunca mudam** entre versões: o envelope (2 bytes de tamanho, 2 de tipo, little-endian) e o começo do `LoginRequest` (tipo 1, com a versão no primeiro campo). Testes de bytes fixos garantem isso.

## Consequências

- Cada mensagem economiza 2 bytes, o que conta no cerco, com centenas de jogadores.
- A conexão inteira fala uma só versão, conferida uma vez, logo no começo.
- O envelope fica congelado para sempre. Se um dia ele precisar mudar (por exemplo, para tamanhos maiores que 64 KB), será preciso um novo tipo de conexão, e não só uma nova versão.

## Alternativas consideradas

- **Versão em cada envelope (o plano original):** 2 bytes a mais em toda mensagem, para conferir algo que não muda durante a conexão.
- **Versão numa mensagem própria, antes do login:** uma ida e volta a mais na rede, sem ganho em relação ao primeiro campo do login.
