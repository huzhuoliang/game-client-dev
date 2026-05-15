---
name: push
description: 提交（可选）并推送到本项目的两个 git remote（GitLab origin + GitHub）。当用户说 "push"、"提交推送"、"推送一下"、"提交一下推送"、"commit and push" 等意图时触发。报告必须只复述实际 git 命令输出，不能脑补另一个 remote 的状态。
---

# 双 remote 推送工作流

本项目的 `origin` remote 应配置为一条命令同时推 GitLab 和 GitHub；`github` remote 只是"只推 GitHub"的备选。执行前以 `git remote -v` 的实际输出为准，确认 `origin` 有两条 `(push)`。

| Remote | 用途 | URL |
|---|---|---|
| `origin` (fetch) | 从 GitLab 拉取 | `gitlab:huzhuoliang/assetbundletest.git` |
| `origin` (push #1) | 推 GitLab | `gitlab:huzhuoliang/assetbundletest.git` |
| `origin` (push #2) | 推 GitHub | `git@github.com:huzhuoliang/game-client-dev.git` |
| `github` | 备选（只推 GitHub） | `git@github.com:huzhuoliang/game-client-dev.git` |

## 执行流程

1. 检查状态：

   ```bash
   git status --short
   ```

   工作树和 index 都干净时，跳到 push。存在改动时，先 commit。

2. Commit：

   - 用户给了 commit 信息就直接用。
   - 用户没给信息时，读 `git diff --staged` / `git diff HEAD` 和 `git log --oneline -20`，拟一条中文标题和可选简短 body。
   - 只 stage 本次相关文件，不用 `git add -A`。
   - commit message 末尾加项目协作签名：

   ```text
   Co-authored-by: Codex <noreply@openai.com>
   ```

   推荐用多段 `-m`，避免 bash heredoc 在 PowerShell 里失效：

   ```bash
   git commit -m "<标题>" -m "<body 可选>" -m "Co-authored-by: Codex <noreply@openai.com>"
   ```

3. Push：

   ```bash
   git push origin main
   ```

   不要用无参数 `git push`，它依赖分支配置，行为不够明确。

## 报告规则

报告必须来自实际 push 输出里的 `To ...` 段落，不能按预期格式补写没出现的 remote。

成功时可以这样汇报：

```text
推完了，commit `<hash>`：
- GitLab: <GitLab 段实际 ref-update 行>
- GitHub: <GitHub 段实际 ref-update 行>
```

如果 exit code 非 0、输出含 `! [rejected]` / `error:`，或预期的两个 `To ...` 段落缺失，立即停下：贴出真实输出，说明哪一边没确认成功，再询问用户要回滚 commit、先 pull --rebase、还是处理 force push 等危险操作。

`git push` 的 stderr 可能出现 LF/CRLF warning，这不算失败；按实际 exit code 和 push 段落判断。

## 何时不该触发本 skill

- 用户只说"commit"不说"push" -> 只 commit，不 push
- 用户说"force push"或"reset" -> 这是危险操作，不在本 skill 范围；按 AGENTS.md 规则单独确认
- 用户在 detached HEAD / 非 main 分支 -> 先告诉用户当前分支，问是否真的要推这个分支
