---
id: 0012
titulo: SDL3 para janela, entrada e GPU, com shaders HLSL compilados pelo DXC
status: proposta
data: 2026-11-17
substitui:
substituida-por:
---

# ADR 0012: SDL3 para janela, entrada e GPU, com shaders HLSL compilados pelo DXC

## Contexto

O [ADR 0006](0006-cliente-windows-primeiro-plataforma.md) deixou a escolha da biblioteca gráfica para a fatia 1, com protótipo. O cliente precisa de janela, mouse, teclado e GPU. As exigências são estas:

- Windows primeiro, sem fechar as portas de Linux e mobile;
- APIs gráficas modernas (Direct3D 12, Vulkan e Metal), para o forward+ e o instancing da ARCHITECTURE;
- funcionar nas GPUs integradas do piso de hardware;
- compatível com NativeAOT (INV-13);
- licença compatível com a MIT;
- uma camada fina, porque o renderizador é nosso.

## Decisão

- **SDL3** (licença zlib) cuida da janela, da entrada e da GPU, pela API de GPU do próprio SDL3. Ela escolhe o Direct3D 12 ou o Vulkan no Windows, e o Metal nos sistemas da Apple.
- O C# fala com o SDL3 pelo pacote **`ppy.SDL3-CS`** (licença MIT, versão fixa em `Directory.Packages.props`). O pacote traz a biblioteca nativa do SDL3 pronta para Windows, Linux, macOS, Android e iOS.
- **Só o projeto `Elysium.Core.Client.Sdl` usa o SDL3** (INV-12). Um teste de arquitetura garante isso a partir da T08 da fatia 1.
- **Shaders em HLSL, compilados antes, pelo DXC da Microsoft:**
  - DXIL, para o Direct3D 12, com o DXC do Windows SDK, que assina o shader com a `dxil.dll`;
  - SPIR-V, para o Vulkan, com o DXC do Vulkan SDK.

  O script `src/core/Elysium.Core.Client.Sdl/Shaders/compile.sh` gera os dois formatos. Os arquivos compilados ficam no repositório (item provisório TD-0010), então nem a CI nem quem só roda o jogo precisa do DXC. O formato da Apple (MSL) é decidido quando o Metal entrar.

## Resultado do protótipo

Feito em 2026-11-17, no computador do dono (T02 da fatia 1): uma janela com um cubo girando, iluminado, com teste de profundidade.

| Pergunta | Resultado |
|---|---|
| Desenha com Direct3D 12? | sim: é o que o SDL3 escolhe sozinho no Windows |
| Desenha com Vulkan? | sim: forçado com a variável de ambiente `SDL_GPU_DRIVER=vulkan` |
| A biblioteca nativa vem no pacote? | sim, para Windows, Linux, macOS, Android e iOS (x64 e ARM) |
| Compatível com NativeAOT? | sim: o build com os analisadores de AOT não dá aviso, e a publicação nativa gerou um programa de 1,2 MB (mais a `SDL3.dll`) que abriu a janela |
| A janela abre no computador do dono? | confirmado pelo dono em: *(pendente)* |

Dois cuidados para quando o cliente for publicado como programa nativo (fatia de distribuição):

- **SDK corrigido:** o compilador NativeAOT do SDK atual (`Microsoft.DotNet.ILCompiler` 10.0.11) tem um alerta de segurança conhecido (GHSA-p3jp-9v83-76w9). Como avisos são tratados como erros, ele bloqueia a publicação nativa. O protótipo só passou desse ponto numa pasta temporária, com o alerta rebaixado para aviso. A publicação de verdade vai precisar de um SDK com a correção.
- **Linker da Microsoft:** a publicação nativa no Windows precisa das ferramentas de C++ do Visual Studio na máquina que gera o programa.

## Consequências

- Portar o cliente para Linux, Android e iOS é ligar o SDL3 nessas plataformas, só no `Elysium.Core.Client.Sdl`. A biblioteca nativa já vem no pacote.
- O renderizador é nosso, em cima da API de GPU do SDL3. Ela esconde as diferenças entre Direct3D 12, Vulkan e Metal, mas não traz malhas, materiais nem cenas: isso a engine escreve.
- A ligação `ppy.SDL3-CS` é mantida pela equipe do osu! para uso próprio. A versão fica fixa, e trocar de ligação mexe num projeto só.
- Quem muda um shader precisa do Windows SDK e do Vulkan SDK para rodar o `compile.sh`. Quem só compila e roda o jogo, não.
- A `SDL3.dll` vai junto com o cliente.

## Alternativas consideradas

- **Outra ligação do SDL3 para C# (pacote `SDL3-CS`):** é a mesma biblioteca por baixo. A da equipe do osu! foi preferida porque traz a biblioteca nativa para todas as plataformas e é usada por um jogo em produção.
- **Silk.NET:** cobre muito mais do que precisamos (OpenGL, OpenAL, OpenXR e outros), e escrever direto em Vulkan e Direct3D 12 daria muito mais código do que usar a API de GPU do SDL3.
- **MonoGame ou FNA:** são frameworks completos, com o próprio loop, a própria pipeline de conteúdo e o próprio jeito de desenhar. Decidiriam por nós partes que a ARCHITECTURE quer controlar (forward+, orçamento de personagens, conteúdo por hash).
- **Raylib:** simples, mas baseada em OpenGL, sem Direct3D 12, Vulkan nem Metal.
- **SDL_shadercross no lugar do DXC:** uma ferramenta só para todos os formatos, inclusive o da Apple. Não está instalada e precisaria ser compilada. O DXC já vem no Windows SDK e no Vulkan SDK. A escolha é revista quando o Metal entrar.
