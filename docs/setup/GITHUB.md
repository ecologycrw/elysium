# Configuração do GitHub

Passo a passo para colocar o Elysium no GitHub com as proteções do [ADR 0008](../adr/0008-fluxo-git.md). Os passos marcados **(dono)** são feitos pelo dono do produto; os marcados **(IA)**, pela IA.

- Repositório: `ecologycrw/elysium` (público).
- Conta do dono: `ecologycrw`, usada **só pelo navegador**.
- Conta da IA: `elysium-ci-dev`, usada pelo `gh` e pelo Git neste PC.

## 1. Criar o repositório (dono, navegador)

1. Em github.com, logado na sua conta: **+ → New repository**.
2. Nome `elysium`, **Public**. **Não** marque README, `.gitignore` nem licença: o repositório precisa nascer vazio.
3. **Create repository**.

## 2. Convidar a conta da IA (dono e conta da IA, navegador)

1. No repositório: **Settings → Collaborators → Add people** → `elysium-ci-dev` → papel **Write**.
2. Numa janela anônima, entre na conta `elysium-ci-dev` e aceite o convite (chega por e-mail ou em github.com/notifications).

## 3. Configurações do repositório (dono, navegador)

Em **Settings**:

| Onde | O que fazer |
|---|---|
| General → Pull Requests | Deixar marcado **só** "Allow squash merging". Marcar "Automatically delete head branches" |
| Actions → General → Workflow permissions | **Read repository contents and packages permissions** |
| Actions → General → Fork pull request workflows | **Require approval for all external contributors** |
| Advanced Security (ou Code security) | Ativar **Secret Protection** e **Push protection** |
| Advanced Security → Private vulnerability reporting | **Enable** |
| Moderation options → Interaction limits | **Limit to repository collaborators** (o máximo de tempo disponível) |

## 4. Trocar o login deste PC para a conta da IA (dono, terminal)

A IA não pode usar a sua conta. Rode no terminal, um comando por vez:

```bash
gh auth logout --hostname github.com --user ecologycrw
```

```bash
gh auth login --hostname github.com --git-protocol https --web --scopes admin:ssh_signing_key
```

O segundo comando mostra um código. Abra o link **na janela anônima logada como `elysium-ci-dev`**, cole o código e autorize. Depois:

```bash
gh auth setup-git
```

```bash
gh auth status
```

O `gh auth status` deve mostrar **só** `elysium-ci-dev`.

Por último, abra o **Gerenciador de Credenciais do Windows → Credenciais do Windows** e remova as entradas `git:https://github.com` que existirem.

## 5. Chave de assinatura dos commits (dono, terminal)

> **Faça este passo antes do primeiro commit do projeto.** A regra da `main` exige commits assinados, e um commit feito antes da chave não pode ser assinado depois sem reescrever o histórico, o que é proibido. Na preparação do Elysium isso aconteceu: os commits foram copiados (`git cherry-pick`) para uma branch nova, já assinados, e o PR foi aberto de novo.

Crie a chave. Quando ela pedir uma senha (*passphrase*), aperte **Enter** duas vezes, sem digitar nada:

```bash
ssh-keygen -t ed25519 -C "elysium-ci-dev signing" -f "$HOME/.elysium_signing_key"
```

Cadastre a chave pública na conta da IA, como chave de assinatura:

```bash
gh ssh-key add "$HOME/.elysium_signing_key.pub" --type signing --title "Elysium signing"
```

Configure o Git **só deste repositório** para assinar (rode dentro da pasta do projeto):

```bash
git config gpg.format ssh
```

```bash
git config user.signingkey "$HOME/.elysium_signing_key.pub"
```

```bash
git config commit.gpgsign true
```

A chave privada (`.elysium_signing_key`, sem `.pub`) nunca sai do seu PC e nunca entra no repositório.

## 6. Enviar a main pela primeira vez (dono, terminal)

A IA adiciona o endereço do repositório (`git remote add origin ...`). Depois, você envia a `main`, uma única vez:

```bash
git push -u origin main
```

Depois disso, ninguém envia nada direto para a `main`: tudo entra por PR.

## 7. Proteger a main (dono, navegador)

**Settings → Rules → Rulesets → New ruleset → Import a ruleset** e escolha o arquivo [`docs/setup/ruleset-main.json`](ruleset-main.json) deste projeto. Confira e clique em **Create**.

A regra exige na `main`:

- PR com 1 aprovação, do code owner, de alguém que não fez o último envio;
- aprovações antigas descartadas a cada novo envio;
- CI verde em Ubuntu e Windows, e os diagramas válidos;
- só squash merge, histórico linear e commits assinados;
- nada de force push nem de apagar a branch;
- nenhuma exceção, nem para o dono.

## 8. PR #1 (IA)

A IA envia a branch `preparacao/ambiente`, abre o PR #1 e confere:

- a CI rodando nos dois sistemas;
- o PR bloqueado até a sua aprovação.

## 9. Revisar e fazer o merge (dono, navegador)

No PR: aba **Files changed → Review changes → Approve**. Depois, **Squash and merge**.

## Renovação

O login do `gh` da conta da IA não expira sozinho. Se algum dia ele sair, repita o passo 4.
