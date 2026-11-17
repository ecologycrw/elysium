---
id: 0006
titulo: Cliente Windows primeiro, com camada de plataforma e código compatível com NativeAOT
status: aceita
data: 2026-11-16
substitui:
substituida-por:
---

# ADR 0006: Cliente Windows primeiro, com camada de plataforma e código compatível com NativeAOT

## Contexto

O público joga principalmente no Windows, mas Linux e mobile são alvos futuros. Isolar o que depende de plataforma custa pouco no começo e é muito caro de fazer depois. No iOS, o .NET precisa compilar tudo antes de rodar (NativeAOT), e reflexão dinâmica não funciona bem lá.

## Decisão

- O cliente começa **só no Windows 64 bits**.
- Janela, entrada, arquivos, áudio e API gráfica ficam atrás de uma **camada de plataforma**. Núcleo e Classic nunca falam com o sistema operacional diretamente.
- Os testes rodam em **Windows e Linux** desde o início, para que nenhum código exclusivo do Windows entre sem ser percebido.
- Todo código de biblioteca é **compatível com NativeAOT**: os projetos ligam `IsAotCompatible`, que ativa os analisadores de AOT e trimming. Reflexão dinâmica e geração de código em tempo de execução são proibidas; usam-se source generators.
- A biblioteca gráfica (a candidata é o SDL3 GPU, que cobre Vulkan, D3D12 e Metal) é escolhida na fatia 1, com protótipo e ADR.
- Pensamentos de mobile que entram desde cedo nas decisões: toque em vez de mouse (clicar para andar vira tocar para andar), UI que se adapta a telas pequenas, menos memória e GPU, e o sistema podendo pausar o jogo a qualquer momento.

## Consequências

- Portar para Linux e mobile depois exige implementar a camada de plataforma, não reescrever a engine.
- Algumas bibliotecas populares que dependem de reflexão não podem ser usadas.
- A CI fica um pouco mais longa por rodar em dois sistemas.

## Alternativas consideradas

- **Escrever direto para o Windows:** mais rápido no começo, porém fecha as portas de Linux e mobile.
- **Implementar várias plataformas desde o início:** custo alto sem retorno agora.
