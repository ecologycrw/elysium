# Visão

> Documento vivo. Mudanças exigem aprovação do dono do produto.

## O que é

O Elysium é uma engine open source (MIT), em C# (.NET 10), para desenvolvedores criarem **MMORPGs no estilo clássico**. A referência de gênero são os MMORPGs de ação do início dos anos 2000, como o Mu Online. Esses jogos têm em comum:

- mapas em grade e câmera fixa em 3/4;
- clicar para andar e combate por alvo;
- progressão por itens com nível e opções;
- eventos agendados;
- uma economia baseada em itens.

O Elysium não tem afiliação com nenhum desses jogos ou empresas. Nenhum nome, asset, arquivo, texto ou protocolo deles é usado (ver [Conteúdo de terceiros](CONVENTIONS.md#conteúdo-de-terceiros)).

"Elysium" é um nome provisório.

## Para quem

Para **desenvolvedores** que querem criar e operar um MMORPG desse estilo sem construir servidor, persistência e rede do zero. O jogo de cada desenvolvedor é feito de **dados, scripts e arte**; as mecânicas vêm prontas e configuráveis.

## Como a engine é construída

A engine é construída junto com um **jogo de demonstração** (Elysium Demo, nome provisório), que é o primeiro cliente dela. Ele serve para três coisas:

- provar que a engine funciona;
- mostrar o que ela faz;
- servir de modelo inicial para quem for criar um jogo.

O jogo de demonstração é uma amostra para testes, não um jogo comercial.

## Pilares

1. **O servidor é o produto principal.** Ele tem autoridade total; o cliente mostra o que o servidor decidiu.
2. **A economia é protegida pela arquitetura.** Um dupe é impossível pelo desenho do sistema, não detectado depois.
3. **A engine define os mecanismos; o jogo define os números e as regras.** Conteúdo é dado.
4. **Feito para hardware modesto.** O público joga muito em PCs populares e GPUs integradas.
5. **Verificável.** Tudo o que uma máquina consegue checar é checado por máquina.

## Pilares do cliente

- Personagens modulares (elmo, armadura, calça, luvas, botas, armas, asas) em grande quantidade na tela.
- Brilho de item por nível e opções, definido por dados.
- Partículas e efeitos de skill com orçamento e redução de qualidade à distância.
- Interface pesada (inventário em grade, trade, lojas, chat, party, guild), com data binding e skins.
- Windows primeiro, sem fechar as portas de Linux e mobile.

## Piso de hardware (proposta; confirmada na fatia de escala)

| | Mínimo | Recomendado |
|---|---|---|
| Sistema | Windows 10 64 bits | Windows 10/11 64 bits |
| CPU | 4 threads (Core i3 de 6ª a 8ª geração, Ryzen 3 2200G) | 6 núcleos |
| RAM | 8 GB | 16 GB |
| GPU | Integrada: Intel UHD 620/630, Radeon Vega 8 | GTX 1050 Ti / 1650 ou equivalente |
| Disco | HD comum | SSD |
| Resolução | 1366×768 | 1920×1080 |
| Meta | 60 FPS caçando; 30 FPS estáveis no cerco, em qualidade baixa | 60 FPS no cerco, em qualidade média |

A **cena de referência** é um cerco com 300 personagens equipados, com asas e skills ao mesmo tempo.

## Não-objetivos

O que o Elysium **não** faz, de propósito:

- **Não é uma engine genérica.** O núcleo é um núcleo de MMO; servir para jogos de corrida, plataforma ou tiro não é objetivo.
- **Sem motor de física.** A colisão é a grade de tiles.
- **Sem console.**
- **Sem mundo aberto com streaming contínuo.** Mapas são separados e carregados no teleporte.
- **Sem ray tracing, iluminação global dinâmica ou geometria virtualizada.**
- **Sem compatibilidade** com clientes, arquivos ou protocolos de outros jogos.
- **Sem linguagem de script própria.**
- **Sem microsserviços no início.** O servidor começa como um processo só.
