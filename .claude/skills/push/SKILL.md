---
name: push
description: 提交（可选）并推送到本项目的两个 git remote（GitLab origin + GitHub）。当用户说 "push"、"提交推送"、"推送一下"、"提交一下推送"、"commit and push" 等意图时触发。**铁律：报告必须只复述实际 git 命令输出，不能脑补另一个 remote 的状态**。
---

# 推送工作流（双 remote 镜像）

本项目的 `origin` remote 被配置成**push 同时上 GitLab 和 GitHub**两个仓库（一条 `git push origin` 推两边）。`github` remote 保留作为"只推 GitHub"的备选。

| Remote | 用途 | URL |
|---|---|---|
| `origin` (fetch) | 从 GitLab 拉取 | `gitlab:huzhuoliang/assetbundletest.git` |
| `origin` (push #1) | 推 GitLab | `gitlab:huzhuoliang/assetbundletest.git` |
| `origin` (push #2) | 推 GitHub | `git@github.com:huzhuoliang/game-client-dev.git` |
| `github` | 备选（只推 GitHub） | `git@github.com:huzhuoliang/game-client-dev.git` |

## 步骤

### 1. 检查待提交状态

```bash
git status --short
```

- **工作树干净 + index 干净** → 跳到步骤 3（只 push）
- **有改动** → 步骤 2

### 2. Commit

按情况构造 commit message：

- 用户给了具体的 commit 信息 → 直接用
- 用户说"提交推送"没给信息 → 跑 `git diff --staged` / `git diff HEAD` 分析改动，**提一条中文标题 + 简短 body**，参考最近 commit 风格（`git log --oneline -20`）
- 必须以 `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` 收尾（项目惯例）

**stage 改动**：只 add 本次相关的文件，**不**用 `git add -A`（避免误带 .env / 临时文件）。

**用 heredoc 传 message** 保格式：

```bash
git commit -m "$(cat <<'EOF'
<标题>

<body 可选>

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### 3. Push（一条命令推两个仓库）

```bash
git push origin main
```

`origin` 配了两个 push URL，stdout 会**分别**输出两段 `To <url>` + ref-update 行。如：

```
To gitlab:huzhuoliang/assetbundletest.git
   74517c2..3522435  main -> main
To github.com:huzhuoliang/game-client-dev.git
   74517c2..3522435  main -> main
```

### 4. 报告

汇报里给两段 push 的真实输出。模板：

```
推完了，commit `<hash>`：
- GitLab: <gitlab 段的实际 ref-update 行>
- GitHub: <github 段的实际 ref-update 行>
```

例：

```
推完了，commit `3522435`：
- GitLab: 74517c2..3522435  main -> main
- GitHub: 74517c2..3522435  main -> main
```

**铁律**（见下文"禁止脑补"小节）：报告里的两行必须分别来自 stdout 里那两段 `To ...` 的内容。**如果 stdout 里只有一段** `To ...`，**只能写一行**——说明配置坏了或某条 push 失败，立刻告警。

如果 push **失败**（exit code 非 0、或输出含 `! [rejected]` / `error:` / 缺少某一段 `To ...`）：

- **立即停下**，不要假装成功
- 把失败原因 + 完整错误输出贴出来
- 询问用户该回滚 commit、force push、还是先 pull --rebase

---

## 铁律：禁止脑补

`git push` 的 stdout **只显示当前推的 remote 一行**。**绝对不允许**根据格式补全另一个 remote 的"看似合理"的输出行。

**正确做法**：

- 跑 `git push origin main` → 拿到 stdout 里的 `xxx..yyy main -> main` → 写进汇报"GitLab origin"那行
- 跑 `git push github main` → 同理写"GitHub"那行
- **看不到的不要写**

---

## 已知坑

- 当前 `origin` 配了多 push URL（见顶部表格），单条 `git push origin main` 推两边。**汇报前必须看到 stdout 里真的有两段 `To ...`**，否则不能写两行。
- 不要跑 `git push`（无参数）——它依赖 `branch.main.remote`，行为可能跟你期望不一致；显式用 `git push origin main`。
- `git push` 的 stderr 上有时会出现 LF/CRLF 警告（`warning: in the working copy of '...', LF will be replaced by CRLF`），那不是 push 失败，正常忽略。
- 如果 `origin` 的多 push 配置被意外清掉（比如手工跑 `git remote set-url --push origin <single-url>` 没带 `--add`），单 push 就只推一个仓库。`git remote -v` 应该总能看到 origin 有 2 条 `(push)` 行；如果只有 1 条，先恢复配置再 push。

---

## 何时不该触发本 skill

- 用户只说"commit"不说"push" → 只做步骤 2，跳过 3-4
- 用户说"force push"或"reset" → 这是危险操作，不在本 skill 范围；按 CLAUDE.md 规则单独确认
- 用户在 detached HEAD / 非 main 分支 → 先告诉用户当前分支，问是否真的要推这个分支
