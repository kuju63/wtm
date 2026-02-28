# wtm - Git Worktree 作成ツール ユーザーガイド

**バージョン**: 1.0.0  
**最終更新**: 2026-01-03

## 目次

1. [概要](#概要)
2. [インストール](#インストール)
3. [基本的な使い方](#基本的な使い方)
4. [コマンドリファレンス](#コマンドリファレンス)
5. [使用例](#使用例)
6. [トラブルシューティング](#トラブルシューティング)
7. [FAQ](#faq)

## 概要

`wtm` は Git worktree の作成を簡単にする CLI ツールです。複雑な Git コマンドを覚える必要なく、ブランチ名を指定するだけで、新しいブランチの作成と worktree の追加を一度に実行できます。

### 主な機能

- **シンプルなコマンド**: `wtm create <ブランチ名>` だけで worktree を作成
- **自動ブランチ作成**: 指定したブランチが存在しない場合は自動的に作成
- **エディター起動**: worktree 作成後、指定したエディターを自動起動（オプション）
- **カスタムパス**: worktree の作成場所を自由に指定可能
- **エラーメッセージ**: わかりやすいエラーメッセージと解決策を表示
- **クロスプラットフォーム**: Windows、macOS、Linux で動作

## インストール

### 前提条件

- Git 2.5 以上
- .NET 10 Runtime

### インストール手順

#### macOS / Linux

```bash
# リポジトリをクローン
git clone https://github.com/your-org/wt.git
cd wt

# ビルド
dotnet build -c Release

# インストール（オプション）
sudo cp wt.cli/bin/Release/net10.0/wtm /usr/local/bin/wtm
sudo chmod +x /usr/local/bin/wtm
```

#### Windows

```powershell
# リポジトリをクローン
git clone https://github.com/your-org/wt.git
cd wt

# ビルド
dotnet build -c Release

# インストール（オプション）
# wt.cli/bin/Release/net10.0/wtm.exe を PATH に追加
```

### インストール確認

```bash
wtm --help
```

正常にインストールされていれば、ヘルプメッセージが表示されます。

## 基本的な使い方

### 最もシンプルな使い方

```bash
wtm create <ブランチ名>
```

これだけで、以下が自動的に実行されます：

1. 現在のブランチをベースに新しいブランチを作成
2. `../worktrees/<ブランチ名>` に worktree を追加
3. 作成された worktree のパスを表示

### 例

```bash
# feature-login という名前のブランチと worktree を作成
wtm create feature-login
```

実行結果：

```shell
✓ Created branch 'feature-login' from 'main'
✓ Added worktree at: /Users/yourname/projects/worktrees/feature-login
→ Next: cd /Users/yourname/projects/worktrees/feature-login
```

## コマンドリファレンス

### コマンド構文

```bash
wtm create <ブランチ名> [オプション]
```

### 必須引数

#### `<ブランチ名>`

作成する新しいブランチの名前を指定します。

- **制約**: Git の命名規則に準拠（英数字、`-`、`_`、`/` のみ）
- **例**: `feature-login`, `bugfix/issue-123`, `user_story_1`

### オプション

#### `--base, -b <ベースブランチ>`

新しいブランチを作成する際のベースとなるブランチを指定します。

- **デフォルト**: 現在のブランチ
- **例**: `--base main`, `-b develop`

```bash
wtm create hotfix-urgent --base production
```

#### `--path, -p <パス>`

worktree を作成する場所を指定します。

- **デフォルト**: `../worktrees/<ブランチ名>`
- **指定可能**: 絶対パスまたは相対パス
- **例**: `--path ~/projects/my-feature`, `-p /tmp/test-worktree`

```bash
wtm create experiment --path ~/experiments/test-feature
```

#### `--editor, -e <エディター>`

worktree 作成後に自動起動するエディターを指定します。

- **指定可能な値**: `vscode`, `vim`, `emacs`, `nano`, `idea`
- **デフォルト**: なし（エディターを起動しない）
- **例**: `--editor vscode`, `-e vim`

```bash
wtm create feature-ui --editor vscode
```

#### `--checkout-existing`

既存のブランチが存在する場合、そのブランチをチェックアウトします。

- **型**: フラグ（値なし）
- **デフォルト**: false（エラーを表示）

```bash
wtm create existing-branch --checkout-existing
```

#### `--output, -o <形式>`

出力形式を指定します。

- **指定可能な値**: `human`（人間可読）、`json`（JSON形式）
- **デフォルト**: `human`
- **例**: `--output json`, `-o json`

```bash
wtm create api-v2 --output json
```

#### `--verbose, -v`

詳細な診断情報を出力します。

- **型**: フラグ（値なし）
- **デフォルト**: false

```bash
wtm create debug-test --verbose
```

#### `--help, -h`

ヘルプメッセージを表示します。

```bash
wtm --help
wtm create --help
```

## 使用例

### 例1: 基本的な worktree 作成

現在のブランチをベースに、デフォルトの場所に worktree を作成します。

```bash
wtm create feature-authentication
```

**実行結果:**

```shell
✓ Created branch 'feature-authentication' from 'main'
✓ Added worktree at: /Users/dev/projects/worktrees/feature-authentication
→ Next: cd /Users/dev/projects/worktrees/feature-authentication
```

### 例2: ベースブランチを指定

`develop` ブランチをベースに worktree を作成します。

```bash
wtm create feature-payment --base develop
```

### 例3: エディターを自動起動

worktree 作成後、VS Code を自動起動します。

```bash
wtm create feature-dashboard --editor vscode
```

**実行結果:**

```shell
✓ Created branch 'feature-dashboard' from 'main'
✓ Added worktree at: /Users/dev/projects/worktrees/feature-dashboard
✓ Launched VS Code
```

### 例4: カスタムパスに作成

ホームディレクトリ配下の任意の場所に worktree を作成します。

```bash
wtm create experiment --path ~/experiments/feature-x
```

### 例5: 既存ブランチをチェックアウト

既に存在するブランチで worktree を作成します。

```bash
wtm create existing-feature --checkout-existing
```

### 例6: JSON 形式で出力

スクリプトから利用しやすい JSON 形式で結果を出力します。

```bash
wtm create api-endpoint --output json
```

**実行結果:**

```json
{
  "success": true,
  "worktree": {
    "path": "/Users/dev/projects/worktrees/api-endpoint",
    "branch": "api-endpoint",
    "baseBranch": "main",
    "createdAt": "2026-01-03T14:30:00Z"
  },
  "editorLaunched": false
}
```

### 例7: 詳細情報を表示

トラブルシューティング用に詳細な実行ログを出力します。

```bash
wtm create test-branch --verbose
```

**実行結果:**

```shell
[DEBUG] Checking for Git repository...
[DEBUG] Found Git repository at: /Users/dev/projects/my-repo
[DEBUG] Current branch: main
[DEBUG] Checking if branch 'test-branch' exists...
[DEBUG] Branch 'test-branch' does not exist
[DEBUG] Creating branch 'test-branch' from 'main'...
[DEBUG] Executing: git branch test-branch main
[DEBUG] Creating worktree at: /Users/dev/projects/worktrees/test-branch
[DEBUG] Executing: git worktree add /Users/dev/projects/worktrees/test-branch test-branch
✓ Created branch 'test-branch' from 'main'
✓ Added worktree at: /Users/dev/projects/worktrees/test-branch
→ Next: cd /Users/dev/projects/worktrees/test-branch
```

### 例8: 複数オプションを組み合わせ

ベースブランチ、カスタムパス、エディター起動を同時に指定します。

```bash
wtm create hotfix-urgent --base production --path ~/hotfixes/urgent --editor vim
```

## トラブルシューティング

### エラー: Git が見つかりません

**エラーメッセージ:**

```shell
✗ Error: Git not found in PATH
→ Solution: Install Git 2.5 or later and ensure it's in your PATH
```

**解決方法:**

1. Git をインストールします
   - macOS: `brew install git`
   - Ubuntu/Debian: `sudo apt install git`
   - Windows: [git-scm.com](https://git-scm.com/) からインストーラーをダウンロード

2. Git が PATH に含まれていることを確認

   ```bash
   git --version
   ```

### エラー: Git リポジトリではありません

**エラーメッセージ:**

```shell
✗ Error: Not a Git repository
→ Solution: Run this command from within a Git repository, or run 'git init' to create one
```

**解決方法:**

1. Git リポジトリ内で実行していることを確認

   ```bash
   git status
   ```

2. まだリポジトリでない場合は初期化

   ```bash
   git init
   ```

### エラー: ブランチが既に存在します

**エラーメッセージ:**

```shell
✗ Error: Branch 'feature-x' already exists
→ Solution: Use --checkout-existing to checkout the existing branch, or choose a different branch name
```

**解決方法:**

オプション1: 既存ブランチをチェックアウト

```bash
wtm create feature-x --checkout-existing
```

オプション2: 別のブランチ名を使用

```bash
wtm create feature-x-v2
```

### エラー: 無効なブランチ名

**エラーメッセージ:**

```shell
✗ Error: Invalid branch name '-invalid'
→ Solution: Branch names must start with alphanumeric character and contain only alphanumeric, '-', '_', '/' characters
```

**解決方法:**

Git の命名規則に従ったブランチ名を使用してください：

- ✅ 正しい例: `feature-login`, `bugfix/issue-123`, `user_story_1`
- ❌ 間違った例: `-invalid`, `feature..test`, `branch name` (スペース含む)

### エラー: パスに書き込み権限がありません

**エラーメッセージ:**

```shell
✗ Error: No write permission to path '/protected/directory'
→ Solution: Choose a different path or check directory permissions
```

**解決方法:**

1. 書き込み可能な別のパスを指定

   ```bash
   wtm create test-branch --path ~/projects/worktrees/test-branch
   ```

2. ディレクトリの権限を確認・修正

   ```bash
   ls -ld /protected/directory
   chmod u+w /protected/directory  # 必要に応じて
   ```

### エラー: エディターが見つかりません

**エラーメッセージ:**

```shell
⚠ Warning: Editor 'vscode' not found in PATH
✓ Created branch 'test' from 'main'
✓ Added worktree at: /Users/dev/projects/worktrees/test
```

**注意**: これは警告のみで、worktree の作成は正常に完了しています。

**解決方法:**

1. エディターが PATH に含まれているか確認

   ```bash
   which code  # VS Code の場合
   which vim   # Vim の場合
   ```

2. VS Code の場合は PATH に追加
   - VS Code を開く
   - Command Palette (Cmd+Shift+P または Ctrl+Shift+P) を開く
   - "Shell Command: Install 'code' command in PATH" を実行

### エラー: ディスク容量不足

**エラーメッセージ:**

```shell
✗ Error: Insufficient disk space
→ Solution: Free up disk space or choose a different location with more available space
```

**解決方法:**

1. ディスク容量を確認

   ```bash
   df -h
   ```

2. 不要なファイルを削除してスペースを確保

3. 別のディスクにパスを指定

   ```bash
   wtm create test --path /mnt/external/worktrees/test
   ```

## FAQ

### Q1: worktree を削除するにはどうすればよいですか？

A: Git の標準コマンドを使用します。

```bash
# worktree を削除
git worktree remove <worktree-path>

# または、ディレクトリを直接削除
rm -rf <worktree-path>
git worktree prune  # クリーンアップ
```

### Q2: 作成した worktree の一覧を確認できますか？

A: Git の標準コマンドで確認できます。

```bash
git worktree list
```

### Q3: デフォルトのエディターを設定できますか？

A: 現在のバージョンでは、デフォルトのエディター設定機能はありません。毎回 `--editor` オプションで指定する必要があります。

将来のバージョンで設定ファイル機能を追加予定です。

### Q4: 複数の worktree を同時に作成できますか？

A: 現在のバージョンでは、一度に1つの worktree しか作成できません。

複数作成する場合は、コマンドを繰り返し実行してください：

```bash
wtm create feature-1
wtm create feature-2
wtm create feature-3
```

### Q5: worktree のパスのデフォルト値を変更できますか？

A: 現在のバージョンでは、デフォルトパス (`../worktrees/<ブランチ名>`) は固定です。

毎回カスタムパスを指定する場合は、シェルエイリアスを使用すると便利です：

```bash
# ~/.bashrc または ~/.zshrc に追加
alias wtc='wtm create --path ~/my-worktrees'
```

使用例：

```bash
wtc feature-login  # ~/my-worktrees/feature-login に作成される
```

### Q6: Windows でパス区切り文字はどうなりますか？

A: `wtm` は自動的にプラットフォームに応じた適切なパス区切り文字を使用します。

- Windows: `\` (バックスラッシュ)
- macOS/Linux: `/` (スラッシュ)

ユーザーはどちらを指定しても正常に動作します。

### Q7: リモートブランチから worktree を作成できますか？

A: 現在のバージョンでは、ローカルブランチのみサポートしています。

リモートブランチから作成する場合は、まずローカルにチェックアウトしてから使用してください：

```bash
git checkout -b feature-remote origin/feature-remote
wtm create feature-remote --checkout-existing
```

### Q8: CI/CD パイプラインで使用できますか？

A: はい、`--output json` オプションを使用すると、スクリプトから扱いやすくなります。

```bash
result=$(wtm create test-branch --output json)
echo $result | jq '.worktree.path'
```

### Q9: worktree 内で別の worktree を作成できますか？

A: 技術的には可能ですが、推奨しません。

worktree は常にメインリポジトリから作成することをお勧めします。

### Q10: ライセンスは何ですか？

A: このプロジェクトは MIT ライセンスの下で公開されています。

詳細は [LICENSE](../../LICENSE) ファイルを参照してください。

## サポート

問題が解決しない場合は、以下の方法でサポートを受けることができます：

1. **GitHub Issues**: [Issues ページ](https://github.com/kuju63/wt/issues) でバグ報告や機能リクエストを投稿
2. **ドキュメント**: [README.md](../../README.md) を参照
3. **コミュニティ**: GitHub Discussions で質問

## 関連リンク

- [Git Worktree 公式ドキュメント](https://git-scm.com/docs/git-worktree)
- [GitHub リポジトリ](https://github.com/kuju63/wt)

---

**最終更新**: 2026-01-03  
**バージョン**: 1.0.0
