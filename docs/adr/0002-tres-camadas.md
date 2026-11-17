---
id: 0002
titulo: Três camadas separadas por projetos
status: aceita
data: 2026-11-16
substitui:
substituida-por:
---

# ADR 0002: Três camadas separadas por projetos

## Contexto

O Elysium é um produto para outros desenvolvedores criarem jogos do mesmo gênero. Cada jogo precisa ser diferente sem que o desenvolvedor tenha de modificar a engine, e a engine precisa continuar pequena e estável.

## Decisão

Dividir o código em três camadas, cada uma formada por projetos .NET separados:

1. **Núcleo (Elysium Core):** servidor por ticks, atores, rede, persistência, ledger, plataforma, renderização, UI e assets. É um núcleo de MMO, não uma engine genérica.
2. **Framework Classic:** as mecânicas do estilo clássico (grade, movimento, combate, itens, inventário, drops, party, guild, eventos, câmera 3/4). É oficial, mas cada peça pode ser estendida ou trocada. "Classic" é um nome provisório.
3. **Jogo:** dados, scripts e arte. O jogo de demonstração é o primeiro.

As dependências só descem: Jogo → Classic → Núcleo.

## Consequências

- O compilador garante a direção: um projeto só enxerga o que referencia.
- Os testes de arquitetura também verificam a direção.
- O núcleo fica pequeno e estável.
- Um desenvolvedor troca uma mecânica sem fazer um fork da engine.
- A IA trabalha com fronteiras claras.
- O custo é que cada fronteira exige uma interface bem pensada.

## Alternativas consideradas

- **Mecânicas dentro do núcleo:** os desenvolvedores teriam de modificar a engine para mudar uma mecânica.
- **Núcleo genérico para qualquer gênero:** um custo enorme sem ganho para o público-alvo.
