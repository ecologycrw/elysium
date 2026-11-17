---
id: 0005
titulo: TCP primeiro, atrás de uma interface de transporte
status: aceita
data: 2026-11-16
substitui:
substituida-por:
---

# ADR 0005: TCP primeiro, atrás de uma interface de transporte

## Contexto

O gênero usa clicar para andar e combate por alvo, com o servidor rodando de 10 a 20 ticks por segundo. UDP com canais confiáveis evita o bloqueio de cabeça de fila do TCP quando há perda de pacotes, mas escrever um protocolo confiável sobre UDP é complexo e cheio de casos difíceis.

## Decisão

- Começar com **TCP**, atrás de uma **interface de transporte** que o resto do código usa sem saber qual transporte está por baixo.
- **Criptografia de verdade (TLS)** antes de qualquer teste aberto ao público. Até lá, a falta de criptografia é registrada como item provisório.
- UDP, QUIC ou WebSocket (para um cliente web) só entram se uma medição mostrar necessidade, como uma implementação nova da mesma interface.

## Consequências

- Muito menos complexidade nas primeiras fatias.
- Trocar o transporte depois não afeta o resto do código.
- Jogadores com conexão instável podem sentir mais atraso. Isso é medido antes de qualquer mudança.

## Alternativas consideradas

- **Protocolo próprio sobre UDP:** complexo demais para o ganho, neste gênero.
- **QUIC desde o início:** dependência e complexidade extras sem evidência de necessidade.
