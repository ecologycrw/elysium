# Relatório da fatia 0: Servidor e bots

## Resumo para o dono

O servidor do jogo existe e funciona: ele roda 20 vezes por segundo, aceita conexões, faz o login e decide sozinho como cada personagem anda e quem vê quem. Nasceu também o bot, um jogador sem tela. Com o comando `demo`, três bots provam o básico de um MMORPG: dois perto se veem, e o terceiro, longe, só vê os outros quando chega perto. Tudo é coberto por 163 testes automáticos, que rodam no Windows e no Linux.

## Critérios de aceitação

| CA | Prova | Resultado |
|---|---|---|
| CA-01 | testes `A_valid_login_is_accepted_with_an_entity_id_and_the_player_joins_the_world` (`GatewayTests`) e `A_player_enters_at_the_spawn_point_and_is_told_its_position` (`MapVisibilityTests`) | passou |
| CA-02 | testes `Another_protocol_version_is_refused_and_the_connection_closed`, `An_invalid_name_is_refused_and_the_connection_closed` e `A_name_already_online_is_refused_ignoring_case_and_free_again_after_leaving` (`GatewayTests`) | passou |
| CA-03 | testes `A_character_walks_one_tile_per_step_interval_until_the_destination`, `Repeating_the_request_every_tick_never_makes_the_character_faster` e `Every_step_goes_to_an_adjacent_tile` (`MapMovementTests`), e `A_move_request_frame_moves_the_character` (`MapActorTests`) | passou |
| CA-04 | testes `An_invalid_destination_is_refused_and_the_character_stays`, `A_destination_further_than_the_path_limit_is_unreachable`, `A_refused_request_stops_the_path_in_progress` e `No_step_ever_lands_on_a_blocked_tile_or_cuts_a_wall_corner` (`MapMovementTests`) | passou |
| CA-05 | testes `Two_players_within_view_see_each_other_appear_with_the_right_position` (`MapVisibilityTests`) e `A_player_sees_the_steps_of_another_player_in_view` (`MapMovementTests`) | passou |
| CA-06 | testes `A_player_out_of_view_receives_nothing_about_the_others` (`MapVisibilityTests`) e `A_far_player_who_walks_into_view_sees_the_others_and_loses_them_when_walking_away` (`MapMovementTests`) | passou |
| CA-07 | testes `A_player_who_leaves_disappears_for_everyone_who_saw_it` e `Leaving_twice_or_leaving_without_joining_does_nothing` (`MapVisibilityTests`), `A_player_who_leaves_through_the_mailbox_disappears_for_the_others` (`MapActorTests`) e `A_client_that_disconnects_leaves_the_world` (`GatewayTests`) | passou |
| CA-08 | testes `A_message_before_the_login_closes_the_connection`, `A_truncated_login_closes_the_connection`, `A_frame_over_the_size_limit_closes_only_that_connection`, `A_client_that_stops_reading_is_disconnected_instead_of_filling_the_server_memory` e `The_world_can_disconnect_a_player` (`GatewayTests`); `A_malformed_message_disconnects_only_that_player`, `A_message_type_the_map_does_not_accept_disconnects_the_player` e `A_player_who_floods_the_mailbox_is_disconnected_and_the_others_keep_playing` (`MapActorTests`) | passou |
| CA-09 | testes `The_same_commands_produce_exactly_the_same_messages_in_two_runs` e `The_same_commands_produce_the_recorded_messages_in_any_process` (`MapActorTests`; o segundo compara com um hash gravado, conferido igual em três processos) | passou |
| CA-10 | testes `Three_bots_run_the_slice_0_scenario_against_the_demo_server` e `The_scenario_passes_again_on_the_same_server_after_the_bots_leave` (`EndToEndTests`), pela rede de verdade; a CI roda em Windows e Linux | passou localmente; a prova no Linux é o check da CI no PR |
| CA-11 | verificação manual: a IA rodou nesta sessão o servidor de demonstração e `demo` com os comandos de "Como o dono testa" e viu a saída esperada, terminando em `PASSED`. Falta a confirmação do dono | aguarda o dono |

## O que você deve testar

São dois grupos de passos. Faça um de cada vez.

**Grupo 1: os testes automáticos.** No terminal, na pasta do projeto, rode:

```bash
dotnet test --solution Elysium.slnx
```

Leva cerca de um minuto. No fim deve aparecer `falhou: 0` e `concluído com êxito: 163`.

**Grupo 2: o servidor e os bots.** Abra **dois** terminais na pasta do projeto.

1. No primeiro, ligue o servidor e deixe-o aberto:

   ```bash
   dotnet run --project samples/Elysium.Demo.Server
   ```

   Deve aparecer uma linha terminando em `Waiting for connections on 127.0.0.1:7000`.

2. No segundo, rode o cenário dos três bots:

   ```bash
   dotnet run --project tools/Elysium.Bot -- demo
   ```

   Leva uns 10 segundos. O texto é em inglês. Confira, nesta ordem:
   - `[Ana] sees Bruno` e `[Bruno] sees Ana`: os dois perto se veem;
   - `[Caio] sees nobody`: o Caio entrou longe;
   - `[Caio] walks to (10, 10)` e depois `[Caio] sees Ana`, `[Ana] sees Caio`: ele chegou perto, e todos se veem;
   - `[Caio] walks to (50, 30)` e depois `lost sight of`: ele se afastou, e eles sumiram;
   - a última linha começa com `PASSED`.

   No primeiro terminal, o servidor mostra `Player Ana joined`, `Player Bruno joined`, `Player Caio joined` e, no fim, três linhas `left: the client closed the connection`.

3. (Opcional) Peça a um bot para andar até um lugar bloqueado:

   ```bash
   dotnet run --project tools/Elysium.Bot -- walk --name Dani --to 25,20
   ```

   Deve aparecer `could not walk to (25, 20): Blocked`, e o bot termina sozinho em poucos segundos.

4. **Desligue o servidor:** clique no primeiro terminal e aperte `Ctrl+C`. Ele deve mostrar `Tick loop stopped` e voltar ao prompt em poucos segundos. **Este passo a IA não conseguiu testar**, então é importante você contar se funcionou.

Depois, conte o que achou. Se algo não sair como descrito, copie o texto do terminal na mensagem.

## Desvios do plano

- **Versão do protocolo (precisa da sua aprovação).** O plano dizia "envelope com tamanho, tipo e versão". A versão ficou só no primeiro campo da mensagem de login, e o envelope e o começo do login nunca mudam entre versões. A decisão está no [ADR 0011](../../docs/adr/0011-versao-do-protocolo-no-login.md), com status `proposta`. Se você aprovar, ele passa a `aceita` na retrospectiva.
- **Sem interface de "relógio da simulação".** A simulação só conta ticks, e o loop de ticks usa o `TimeProvider` do .NET. Isso cumpre o INV-04 sem nenhuma exceção de API proibida.
- **Sem RNG injetado.** A spec diz "relógio e RNG injetados", mas nada nesta fatia sorteia. O RNG nasce com o primeiro sorteio (drops, na fatia 2).
- **Sem ID de mapa no gateway.** Com um mapa só, o mundo é o próprio ator do mapa. O ID entra com o segundo mapa.
- **Limites que o plano não previa:** limite de trabalho por tick e por jogador (`MapActorLimits`) e fila de saída limitada por cliente (`MaxQueuedFramesPerClient`). Vieram da revisão (I1 e I2).
- **Ordem das tarefas:** a T09 (conexão do cliente) foi feita antes da T08 (gateway), porque os testes do gateway a usam. A fila do ator, `ITickable`, `IWorld` e `IClientOutbox` nasceram nas tarefas que primeiro precisaram deles (T04 e T06).

## Revisão

Revisão independente pelo subagente `revisor`, com o pedido do dono de revisar "como um sênior revisaria um júnior". Não houve achado crítico. Houve 4 achados importantes, 9 menores e 7 de estilo, e todos procediam. A tabela completa está na seção "Revisão" do [tasks.md](tasks.md). Em resumo:

- **I1:** um cliente podia congelar o mapa com pedidos válidos. Corrigido com limites no ator.
- **I2:** um cliente que não lia fazia a memória crescer e travava o desligamento. Corrigido com a fila de saída limitada e a escrita interrompível.
- **I3:** desvios do protocolo e do plano. Registrados acima e no ADR 0011, para a sua decisão.
- **I4:** faltavam testes de bytes fixos do protocolo. Criados.
- **Menores:** quase todos corrigidos. M1 virou o TD-0008.
- **Estilo:** a visibilidade saiu da `MapSimulation` para a `AreaOfInterest`, e os métodos longos foram divididos. A parte do E4 que sobrou (mudanças tipadas no `WorldView`) ficou para a fatia 1, no TD-0004.

## Travas alteradas

| Trava | Mudança | Aprovação |
|---|---|---|
| `Directory.Packages.props` | versões de `Microsoft.Extensions.Logging.Abstractions` e `.Console` (10.0.6) | aprovada pelo dono com a spec, em 2026-11-17 |
| `tests/Elysium.Architecture.Tests/Elysium.Architecture.Tests.csproj` | referência a `Elysium.Demo.Server`, para as regras de camadas valerem também para ele | aprovada pelo dono com a spec, em 2026-11-17 |

## Testes removidos

Nenhum teste foi apagado. Na revisão, três mudanças em testes:

- **Marcas de critério retiradas (M5).** Saíram de testes de unidade que não provam o critério (por exemplo, um teste de leitura de bytes marcado com CA-08). Os testes continuam iguais; os critérios seguem provados pelos testes do gateway e do mapa.
- **Asserção trivial retirada (M6).** O teste antigo do loop de ticks tinha uma asserção sobre ticks sobrepostos que era sempre verdadeira. Esse teste, que usava o relógio real, foi substituído por testes com relógio controlado, que verificam a quantidade exata de ticks por segundo.
- **Teste do determinismo ajustado (CA-09).** O roteiro mandava dois pedidos de movimento do Bruno no mesmo tick. Com a correção do I1, só o último pedido de cada tick vale, então os dois pedidos foram separados em dois ticks seguidos.

## Itens provisórios criados

TD-0001 a TD-0008 em [docs/TECH_DEBT.md](../../docs/TECH_DEBT.md):

| Item | Assunto | Destino |
|---|---|---|
| TD-0001 | serialização escrita à mão | fatia 2 |
| TD-0002 | login sem conta | fatia 2 |
| TD-0003 | sem TLS | fatia 2 |
| TD-0004 | estado do mundo dentro do bot | fatia 1 |
| TD-0005 | mapa em texto simples | fatia 2 |
| TD-0006 | regra de nome fixa no núcleo | fatia 2 |
| TD-0007 | sem tempo máximo de login nem limite de conexões | fatia 2 |
| TD-0008 | IDs de entidade criados pelo gateway | fatia 2 |

## Erros e lições

Primeira ocorrência de cada um (a escada do `docs/WORKFLOW.md`):

1. **Mudança de protocolo sem pedir aprovação na hora.** A IA mudou onde a versão do protocolo fica e anotou isso só "para o report". O certo era parar e perguntar, porque mudança de protocolo exige aprovação do dono. A revisão pegou.
2. **Filas sem limite.** O plano não pensou em um cliente abusivo, e as duas filas (de entrada no mapa e de saída para o cliente) nasceram sem limite. A revisão pegou (I1 e I2).
3. **Testes de ida e volta não protegem o formato.** Escrever e ler com o mesmo código não detecta uma mudança nos bytes. Agora cada mensagem tem um teste de bytes fixos.
4. **Pausa fixa num teste,** contra o que o próprio plano dizia. Trocada por espera por condição.
5. **Erros pegos pelos testes durante a implementação:** uma API de fila que não existia no modo usado, caminhos em zigue-zague e o `walk` esperando 60 segundos à toa num destino bloqueado.

## Propostas geradas

- [P-0001](../../docs/PROPOSALS.md): o `/planejar` move a fatia escolhida para "Agora" no ROADMAP.
- [P-0002](../../docs/PROPOSALS.md): régua de qualidade sênior.
- [P-0003](../../docs/PROPOSALS.md): testes de mutação com Stryker.NET.
