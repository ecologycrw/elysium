---
id: 0001
titulo: Dois jogadores se veem
status: aprovada
aprovada-em: 2026-11-17
---

# Fatia 1: Dois jogadores se veem

## Resumo para o dono

Nasce o **cliente do jogo**: um programa com janela, que mostra o mapa em 3D com a câmera 3/4 e um personagem simples (um bloco colorido). Você clica num lugar do chão e o personagem anda até lá, na velocidade e pelo caminho que o servidor decidir. Com duas janelas abertas, cada jogador vê o outro andando. A fatia também liga os **testes de mutação** (proposta P-0003), que provam que os testes do servidor pegam erros de verdade.

## Escopo

- **Biblioteca gráfica:** um protótipo curto escolhe a biblioteca de janela, entrada e gráficos. A candidata é o **SDL3** (com a parte de GPU dele), usado pelo C# por meio do pacote `ppy.SDL3-CS`. A escolha vira o ADR 0012, que você aprova antes de o resto do cliente ser construído em cima dela.
- **Camada de plataforma:** janela, mouse e GPU ficam num projeto próprio (`Elysium.Core.Client.Sdl`). O resto do cliente não sabe que o SDL existe (INV-12), e um teste de arquitetura garante isso.
- **Cliente do jogo de demonstração** (`samples/Elysium.Demo.Client`): conecta no servidor, faz o login com o nome dado e abre a janela. O título da janela mostra o nome do personagem e os quadros por segundo (FPS).
- **Terreno:** o mapa da fatia 0 desenhado em 3D, um quadrado por tile. Os tiles bloqueados aparecem como blocos mais altos e de outra cor.
- **Câmera 3/4:** inclinada e em ângulo fixo, como nos MMORPGs clássicos, seguindo o próprio personagem.
- **Personagem simples:** um bloco em pé. O seu tem uma cor; os outros jogadores, outra.
- **Clicar para andar:** o clique no chão vira um pedido de destino para o servidor. Uma marca aparece no tile clicado e mostra quando o servidor recusa o destino.
- **Movimento suave:** o servidor anda tile por tile, e o cliente desliza o personagem entre um tile e outro, sem nunca ir além do último tile que o servidor confirmou (INV-01).
- **Conexão caída:** se o servidor cair, a janela continua aberta, sem travar, e o título avisa que a conexão caiu.
- **Estado do mundo do cliente** (`Elysium.Classic.Client`): o que o bot sabia sobre o mundo (quem está à vista e onde) sai do bot e vai para este projeto, que o bot e o cliente gráfico passam a usar. Isso resolve o item provisório TD-0004.
- **Quadro sem lixo de memória:** atualizar o mundo e montar a cena de cada quadro não cria memória nova depois que o jogo aquece. Um teste mede isso, como pede a ARCHITECTURE.
- **Testes de mutação (P-0003):** o Stryker.NET muda pedacinhos do código dos projetos de servidor (`Elysium.Core.Server` e `Elysium.Classic.Server`) e confere se algum teste falha. A nota é a porcentagem de mudanças que os testes pegaram. Ele roda na CI em todo PR, e o PR fica vermelho se a nota ficar abaixo do mínimo que você aprovar (a proposta é 80%).

## Fora do escopo

- Nome em cima da cabeça e qualquer texto dentro da janela: nascem com a interface, na fatia 2 (inventário).
- Personagem com modelo 3D, animação e equipamentos: numa fatia de fundação antes das fatias que precisarem disso.
- Iluminação avançada (forward+), sombras e efeitos: nas fatias de renderização e de escala.
- Girar ou aproximar a câmera: numa fatia futura, se o jogo precisar.
- Previsão de movimento no cliente (o personagem começar a andar antes de o servidor responder): só se uma medição em rede real mostrar que é necessária.
- Tela de login com campos para digitar: o nome vem da linha de comando até existirem contas (fatia 2).
- Confirmar o piso de hardware medindo FPS num PC mínimo: fica para a fatia de escala, com a cena do cerco (ver "Decisões" no plano).
- Linux e mobile no cliente: o código não fecha essas portas, mas o cliente só é testado no Windows.

## Critérios de aceitação

- CA-01: O cliente de demonstração conecta, faz o login com o nome dado e abre uma janela com o terreno do mapa em 3D, na câmera 3/4, centrada no próprio personagem. Tiles livres e bloqueados se distinguem. Um nome recusado (vazio, longo ou já em uso) mostra o motivo no terminal e o cliente termina sem abrir a janela.
- CA-02: Clicar num tile livre do chão envia ao servidor o pedido de destino daquele tile. O tile certo é provado por teste, com a câmera em várias posições e tamanhos de janela.
- CA-03: O personagem desenhado anda até o destino de forma suave, na velocidade do jogo, e nunca passa do último tile confirmado pelo servidor. Se o cliente ficar para trás (por exemplo, a janela foi arrastada e travou um pouco), ele alcança a posição do servidor em vez de acumular atraso.
- CA-04: Clicar num tile bloqueado ou fora do alcance faz o servidor recusar o pedido. O personagem não sai do lugar, e a marca do destino mostra a recusa.
- CA-05: Dois clientes no mesmo mapa, perto um do outro, se veem: cada um vê o personagem do outro, na cor dos outros jogadores, aparecer e andar. Quando um sai, o personagem dele some para o outro. Provado por um teste automático pela rede de verdade, com dois clientes sem janela.
- CA-06: Se o servidor cair, o cliente não trava nem fecha sozinho: o título da janela passa a dizer que a conexão caiu, e a janela fecha normalmente quando você quiser.
- CA-07: Depois de aquecido, cada quadro do cliente (aplicar as mensagens do servidor, mover os personagens e montar a cena) não cria memória nova. Um teste mede isso em centenas de quadros.
- CA-08: Só o projeto de plataforma (`Elysium.Core.Client.Sdl`) usa o SDL. Um teste de arquitetura falha se qualquer outro projeto da engine ou do jogo usar o SDL diretamente.
- CA-09: O bot usa o mesmo estado do mundo do cliente gráfico, que agora informa as mudanças de forma organizada (entrou, apareceu, andou, sumiu, destino recusado). O cenário dos três bots continua passando, no Windows e no Linux.
- CA-10: Os testes de mutação rodam na CI sobre `Elysium.Core.Server` e `Elysium.Classic.Server`. A nota de cada um aparece no resultado do PR, e o PR falha se a nota ficar abaixo do mínimo aprovado.
- CA-11: Verificação manual: você liga o servidor e dois clientes, clica para andar nas duas janelas, vê cada personagem andar nas duas e conta o FPS e a placa de vídeo do seu computador.

## Como o dono testa

São quatro grupos de passos. Faça um de cada vez.

**Grupo 1: os testes automáticos.** No terminal, na pasta do projeto:

```bash
dotnet test --solution Elysium.slnx
```

No fim deve aparecer `falhou: 0`.

**Grupo 2: o servidor e duas janelas.** Abra **três** terminais na pasta do projeto.

1. No primeiro, ligue o servidor e deixe-o aberto:

   ```bash
   dotnet run --project samples/Elysium.Demo.Server
   ```

2. No segundo, abra o cliente da Ana:

   ```bash
   dotnet run --project samples/Elysium.Demo.Client -- --name Ana
   ```

   Abre uma janela com o chão do mapa visto de cima, inclinado, e um bloco no meio: é a Ana. O título da janela mostra `Ana` e o FPS.

3. No terceiro, abra o cliente do Bruno:

   ```bash
   dotnet run --project samples/Elysium.Demo.Client -- --name Bruno
   ```

   As duas janelas mostram dois blocos: o próprio personagem numa cor e o outro em outra.

4. Clique no chão da janela da Ana, perto do personagem. Confira:
   - aparece uma marca no tile clicado;
   - a Ana desliza até lá, sem pular de tile em tile, e a câmera a acompanha;
   - na janela do Bruno, a Ana anda do mesmo jeito.

5. Clique num bloco alto (tile bloqueado). A marca mostra a recusa, e a Ana não sai do lugar.

6. Faça o mesmo na janela do Bruno.

7. Feche a janela do Bruno. Na janela da Ana, o Bruno some.

**Grupo 3: o servidor caindo.** Com a janela da Ana ainda aberta, clique no terminal do servidor e aperte `Ctrl+C`. A janela da Ana não trava, e o título passa a dizer que a conexão caiu. Depois, feche a janela.

**Grupo 4: conte o que viu.** Na mensagem, conte:
- se tudo saiu como descrito (se não, copie o texto do terminal);
- o FPS que aparecia no título;
- o modelo da placa de vídeo do seu computador. No Windows: botão Iniciar, digite `Gerenciador de Dispositivos`, abra **Adaptadores de vídeo** e copie o nome que aparece.

Os testes de mutação você não precisa rodar: eles aparecem como um check no PR, com a nota.
