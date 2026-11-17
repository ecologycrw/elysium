# Itens provisórios

Tudo o que foi simplificado de propósito e ainda precisa da versão definitiva. Todo item tem uma fatia de destino (`fatia N`), e a checagem automática recusa um item sem destino. Quando o item é resolvido, ele sai desta tabela e a resolução fica registrada no report da fatia.

| ID | Item provisório | Por que é provisório | Destino | Origem |
|---|---|---|---|---|
| TD-0001 | Serialização das mensagens escrita à mão (`Write` e `Read` em cada mensagem) | são poucas mensagens; o gerador na compilação vale a pena quando elas se multiplicarem | fatia 2 | fatia 0, T01 a T03 |
| TD-0002 | Login só com o nome do personagem, sem conta nem senha | ainda não há banco de dados para guardar contas | fatia 2 | fatia 0, T08 |
| TD-0003 | Conexão sem criptografia (TLS) | nada é aberto ao público antes disso (ADR 0005); a `GameConnection` e o gateway já funcionam sobre qualquer `Stream` | fatia 2 | fatia 0, T08 |
| TD-0005 | Mapa num arquivo de texto simples, sem schema nem build de conteúdo | é o único tipo de dado até agora | fatia 2 | fatia 0, T03 |
| TD-0006 | Regra de caracteres do nome fixa no núcleo (só letras e números ASCII) | o jogo deve poder escolher a regra quando houver contas e personagens salvos | fatia 2 | fatia 0, T08 |
| TD-0007 | Sem tempo máximo para fazer o login e sem limite de conexões: uma conexão que nunca manda nada fica aberta | o servidor só escuta na própria máquina; precisa estar resolvido antes de qualquer teste aberto, junto com o TLS | fatia 2 | fatia 0, revisão (I1, M9) |
| TD-0008 | O gateway cria o `EntityId` de cada conexão com um contador próprio | com um só tipo de entidade não há conflito; quando o mapa criar monstros, os IDs precisam vir de um alocador único (ou de faixas separadas), senão um monstro e um jogador podem ter o mesmo ID | fatia 2 | fatia 0, revisão (M1) |
| TD-0009 | Os testes de mutação usam o executor `mtp` do Stryker.NET, que o próprio Stryker ainda marca como preview | é o único modo que funciona com o xUnit v3 no Microsoft Testing Platform; o modo padrão (`vstest`) não encontra os testes. No início da fatia 2, conferir se uma versão nova tirou o modo do preview e atualizar a versão fixa. Se o job falhar por erro do Stryker, seguir "Se o job `mutation` falhar", em `docs/setup/DEV.md` | fatia 2 | fatia 1, T01 |
| TD-0010 | Shaders compilados à mão (`Shaders/compile.sh`, com o DXC) e guardados compilados no repositório | são poucos shaders; a compilação entra no build de conteúdo (TD-0005), e então a CI confere que os compilados correspondem ao HLSL | fatia 2 | fatia 1, T02 (ADR 0012) |
