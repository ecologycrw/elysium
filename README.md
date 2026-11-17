# Elysium

> Nome provisório. *Elysium is an open source C# engine for building classic-style MMORPGs. Documentation is currently in Portuguese.*

O Elysium é uma engine open source, escrita em C# (.NET 10), para desenvolvedores criarem MMORPGs no estilo clássico: mapas em grade, clicar para andar, combate por alvo, itens com nível e opções, drops, party, guild e eventos agendados.

A engine tem três camadas:

- **Núcleo (Elysium Core):** servidor autoritativo por ticks, rede, persistência, renderização e UI.
- **Framework Classic:** as mecânicas do gênero, prontas e configuráveis por dados.
- **Jogo:** os dados, scripts e arte de cada jogo feito com a engine.

## Estado atual

**Em preparação.** Ainda não há nada jogável. Veja [docs/STATUS.md](docs/STATUS.md) e [docs/ROADMAP.md](docs/ROADMAP.md).

## Documentação

- [Visão](docs/VISION.md): o que o produto é e o que ele não é
- [Arquitetura](docs/ARCHITECTURE.md): como as peças se encaixam
- [Fluxo de trabalho](docs/WORKFLOW.md): como o projeto é desenvolvido
- [Decisões (ADRs)](docs/adr/README.md): por que cada decisão foi tomada
- [Glossário](docs/GLOSSARY.md): os termos usados no projeto

## Desenvolvimento assistido por IA

Quase todo o código deste projeto é escrito por IA, com revisão e aprovação humana. Os commits da IA são feitos por uma conta de máquina e identificados como coautoria do Claude. Veja [docs/WORKFLOW.md](docs/WORKFLOW.md).

## Contribuições

Ainda não estão abertas. Isso vai mudar quando a base estiver estável.

## Segurança

Para relatar uma vulnerabilidade, veja [SECURITY.md](SECURITY.md).

## Licença

[MIT](LICENSE).

## Aviso

O Elysium é um projeto independente, sem afiliação com nenhuma empresa, jogo ou marca. Nenhum nome, asset, arquivo ou protocolo de outros jogos é usado. Marcas citadas na documentação pertencem aos seus donos.
