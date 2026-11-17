# Decisões de arquitetura (ADRs)

Cada ADR registra uma decisão importante, o motivo e as alternativas. Um ADR aceito não é editado: se a decisão mudar, um ADR novo o substitui e diz isso no front matter (`substitui` / `substituida-por`). O modelo fica em `templates/adr.md`.

| ADR | Decisão | Status |
|---|---|---|
| [0001](0001-csharp-dotnet-10.md) | C# e .NET 10 em tudo | aceita |
| [0002](0002-tres-camadas.md) | Três camadas separadas por projetos | aceita |
| [0003](0003-servidor-autoritativo-ator-por-mapa.md) | Servidor autoritativo com um ator por mapa | aceita |
| [0004](0004-persistencia-anti-dupe.md) | Persistência à prova de dupe | aceita |
| [0005](0005-tcp-primeiro.md) | TCP primeiro, atrás de uma interface de transporte | aceita |
| [0006](0006-cliente-windows-primeiro-plataforma.md) | Cliente Windows primeiro, com camada de plataforma e código compatível com NativeAOT | aceita |
| [0007](0007-repositorio-publico-mit.md) | Repositório público com licença MIT | aceita |
| [0008](0008-fluxo-git.md) | Fluxo Git com conta de máquina e merge só pelo dono | aceita |
| [0009](0009-ferramentas-de-verificacao.md) | Ferramentas de verificação | aceita |
| [0010](0010-documentacao.md) | Documentação como memória do projeto | aceita |
| [0011](0011-versao-do-protocolo-no-login.md) | Versão do protocolo na mensagem de login, não em cada envelope | aceita |
| [0012](0012-sdl3-plataforma-e-graficos.md) | SDL3 para janela, entrada e GPU, com shaders HLSL compilados pelo DXC | proposta |
