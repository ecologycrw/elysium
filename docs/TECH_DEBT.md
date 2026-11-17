# Itens provisórios

Tudo o que foi simplificado de propósito e ainda precisa da versão definitiva. Todo item tem uma fatia de destino (`fatia N`), e a checagem automática recusa um item sem destino. Quando o item é resolvido, ele sai desta tabela e a resolução fica registrada no report da fatia.

| ID | Item provisório | Por que é provisório | Destino | Origem |
|---|---|---|---|---|
| TD-0001 | Serialização das mensagens escrita à mão (`Write` e `Read` em cada mensagem) | são poucas mensagens; o gerador na compilação vale a pena quando elas se multiplicarem | fatia 2 | fatia 0, T01 a T03 |
| TD-0002 | Login só com o nome do personagem, sem conta nem senha | ainda não há banco de dados para guardar contas | fatia 2 | fatia 0, T08 |
| TD-0003 | Conexão sem criptografia (TLS) | nada é aberto ao público antes disso (ADR 0005); a `GameConnection` e o gateway já funcionam sobre qualquer `Stream` | fatia 2 | fatia 0, T08 |
| TD-0004 | Estado do mundo visto pelo bot dentro do próprio bot (`WorldView`), que ainda devolve frases em inglês para o terminal | `Elysium.Classic.Client` nasce com o cliente gráfico; ao mudar para lá, `Apply` deve devolver mudanças tipadas (entrou, apareceu, sumiu) e o bot formatar o texto | fatia 1 | fatia 0, T10 e revisão (E4) |
| TD-0005 | Mapa num arquivo de texto simples, sem schema nem build de conteúdo | é o único tipo de dado até agora | fatia 2 | fatia 0, T03 |
| TD-0006 | Regra de caracteres do nome fixa no núcleo (só letras e números ASCII) | o jogo deve poder escolher a regra quando houver contas e personagens salvos | fatia 2 | fatia 0, T08 |
| TD-0007 | Sem tempo máximo para fazer o login e sem limite de conexões: uma conexão que nunca manda nada fica aberta | o servidor só escuta na própria máquina; precisa estar resolvido antes de qualquer teste aberto, junto com o TLS | fatia 2 | fatia 0, revisão (I1, M9) |
| TD-0008 | O gateway cria o `EntityId` de cada conexão com um contador próprio | com um só tipo de entidade não há conflito; quando o mapa criar monstros, os IDs precisam vir de um alocador único (ou de faixas separadas), senão um monstro e um jogador podem ter o mesmo ID | fatia 2 | fatia 0, revisão (M1) |
