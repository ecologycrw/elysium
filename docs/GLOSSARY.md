# Glossário

Termos usados no projeto, em português simples.

| Termo | Significado |
|---|---|
| **ADR** | *Architecture Decision Record*: um registro curto de uma decisão importante, com o motivo e as alternativas. Fica em `docs/adr/` |
| **AOI** | Área de interesse: o pedaço do mapa que um jogador "enxerga". O servidor só envia a ele o que acontece ali |
| **Asset** | Tudo o que não é código: modelos 3D, animações, texturas, ícones, sons |
| **Ator** | Uma parte do servidor que roda sozinha, numa linha de execução só, e conversa com o resto apenas por mensagens. No Elysium, cada mapa é um ator |
| **Bot headless** | Um cliente sem tela, que joga sozinho seguindo um roteiro. Serve para testar o servidor automaticamente |
| **Branch** | Uma linha de trabalho paralela no Git. Cada fatia é feita na sua branch, sem mexer na versão oficial |
| **CA** | Critério de aceitação: uma frase verificável que diz quando uma fatia está pronta (por exemplo, `CA-01`) |
| **CI** | Integração contínua: os testes que rodam nas máquinas do GitHub a cada envio de código |
| **Classic** | O framework do estilo clássico: as mecânicas do gênero. Nome provisório |
| **Commit** | Um ponto de salvamento no histórico do Git |
| **Conta de máquina** | A conta do GitHub usada só pela IA. Ela envia código e abre PRs, mas não aprova nem faz merge |
| **Determinístico** | Com as mesmas entradas, o resultado é sempre o mesmo. Permite repetir exatamente um teste |
| **Dupe** | Duplicação ilegal de item, normalmente explorando uma queda do servidor ou uma troca mal feita |
| **Fatia** | Uma etapa pequena e completa do desenvolvimento, que entrega algo que dá para ver funcionando |
| **Hook** | Um script que roda automaticamente antes ou depois de uma ação (um commit, um comando da IA) e pode bloqueá-la |
| **Hot reload** | Recarregar dados ou código com o programa rodando, sem reiniciar |
| **Instância** | Uma cópia temporária de um mapa, criada para um evento e destruída no fim |
| **Invariante** | Uma regra que nenhuma mudança pode violar (`INV-01`…, em [ARCHITECTURE.md](ARCHITECTURE.md#invariantes)) |
| **Lease de posse** | Um "crachá" que diz qual ator controla um personagem naquele momento. Só um ator tem o crachá por vez |
| **Ledger** | O livro-razão dos itens: o registro de toda criação, transferência e destruição. Só recebe novos registros; nada é apagado |
| **`main`** | A branch principal: a versão oficial, aprovada pelo dono |
| **Merge (squash)** | Juntar uma branch na `main`. O *squash* transforma todos os commits da branch num commit só |
| **NativeAOT** | Compilar o programa C# inteiro antes de rodar. É obrigatório no iOS e por isso orienta como o código é escrito |
| **Núcleo** | A camada mais baixa da engine (Elysium Core): servidor, rede, persistência, renderização. Não sabe nada de gênero ou de jogo |
| **PR** | *Pull Request*: o pedido para juntar uma branch na `main`. É onde o dono revisa e aprova |
| **Protocolo** | As mensagens que cliente e servidor trocam, e o formato delas |
| **Push** | Enviar os commits para o GitHub |
| **Snapshot** | Uma foto do estado do jogo gravada no banco de tempos em tempos |
| **Spec** | A especificação de uma fatia: o que ela entrega e como saber que está pronta |
| **Tick** | Um "passo" da simulação do servidor. O servidor roda de 10 a 20 ticks por segundo |
| **Trava** | Qualquer proteção automática: hook, permissão, teste de arquitetura, CI, regra do GitHub |
