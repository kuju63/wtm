# Contract: インストールスクリプト インターフェース仕様

## 1. Unix インストールスクリプト (`scripts/install.sh`)

### インターフェース

```bash
curl -fsSL https://kuju63.github.io/wt/install.sh | sh
# または
curl -fsSL https://kuju63.github.io/wt/install.sh | sh -s -- --prefix /usr/local
# 強制上書き（--force）
curl -fsSL https://kuju63.github.io/wt/install.sh | sh -s -- --force
# 組み合わせ
curl -fsSL https://kuju63.github.io/wt/install.sh | sh -s -- --prefix /usr/local --force
```

### 引数

| オプション | 説明 | デフォルト |
|---|---|---|
| `--prefix <DIR>` | インストール先ディレクトリ | `${HOME}/.local/bin` |
| `--force` | 既存バイナリへの上書き確認プロンプトをスキップ（FR-017） | `false` |

### インタラクティブ/非インタラクティブ挙動（FR-016/017/018）

| 条件 | 挙動 |
|---|---|
| 既存バイナリなし | 確認なしにインストール |
| `--force` フラグあり | 確認なしに上書き |
| 既存バイナリあり + TTY（インタラクティブ）| `y/N` 確認プロンプト（デフォルト拒否）|
| 既存バイナリあり + 非TTY（パイプ経由等）| 上書きをスキップし `--force` 使用方法を案内 |

### 終了コード

| Code | 意味 |
|---|---|
| `0` | 成功（インストール完了 または 最新版インストール済みによるスキップ） |
| `1` | エラー（非対応プラットフォーム / ネットワークエラー / SHA256失敗 等） |

### 標準出力（正常系）

```
Detecting platform... linux-x64
Fetching latest version... v1.2.3
wtm v1.2.0 is already installed. Updating to v1.2.3...
  ↳ または: wtm v1.2.3 is already the latest version. Skipping.
Downloading wtm v1.2.3 for linux-x64...
Verifying SHA256 checksum...
Installing to /home/user/.local/bin/wtm...
chmod +x /home/user/.local/bin/wtm

✓ wtm v1.2.3 installed successfully!

Next steps:
  wtm --version   # verify installation
  wtm --help      # see available commands
```

### 標準出力（PATH未登録時の追加案内）

```
Note: /home/user/.local/bin is not in your PATH.
To add it, run:
  echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
  source ~/.bashrc
```

### 標準出力（macOS Gatekeeper 案内）

```
Note: On macOS, if you see "cannot be opened because the developer cannot be verified":
  xattr -d com.apple.quarantine /home/user/.local/bin/wtm
```

### 標準出力（上書き確認プロンプト - インタラクティブ TTY）

```
/home/user/.local/bin/wtm already exists (current: v1.2.0, new: v1.2.3).
Overwrite existing /home/user/.local/bin/wtm? [y/N]
```

```
Installation cancelled.
```

### 標準出力（上書きスキップ - 非インタラクティブ環境）

```
Note: /home/user/.local/bin/wtm already exists. Skipping overwrite in non-interactive mode.
To force overwrite, re-run with --force:
  curl -fsSL https://kuju63.github.io/wt/install.sh | sh -s -- --force
```

### 標準エラー出力（異常系）

```
Error: Unsupported platform: freebsd
Supported platforms: linux-x64, linux-arm, macos-arm64
Manual install: https://github.com/kuju63/wt/releases
```

```
Error: SHA256 verification failed for wtm-v1.2.3-linux-x64.
       Downloaded file has been removed. Please retry.
```

```
Error: Failed to download binary. Please check your network connection.
       URL: https://github.com/kuju63/wt/releases/download/v1.2.3/wtm-v1.2.3-linux-x64
```

---

## 2. Windows インストールスクリプト (`scripts/install.ps1`)

### インターフェース

```powershell
irm https://kuju63.github.io/wt/install.ps1 | iex
# または（パラメータ付き）
& ([scriptblock]::Create((irm https://kuju63.github.io/wt/install.ps1))) -Prefix "$env:ProgramFiles\wtm"
# 強制上書き（-Force）
& ([scriptblock]::Create((irm https://kuju63.github.io/wt/install.ps1))) -Force
```

### パラメータ

| パラメータ | 説明 | デフォルト |
|---|---|---|
| `-Prefix <path>` | インストール先ディレクトリ | `$env:LOCALAPPDATA\Programs\wtm` |
| `-Force` | 既存バイナリへの上書き確認プロンプトをスキップ（FR-017） | `$false` |

### インタラクティブ/非インタラクティブ挙動（FR-016/017/018）

PowerShell は `[Environment]::UserInteractive` で対話モードを判定。`irm | iex` は現在のセッション内でスクリプトを実行するため `[Environment]::UserInteractive` の値は変化しない。インタラクティブなターミナルから実行した場合は `$true` のまま確認プロンプトが表示される。非インタラクティブ環境（サービス、タスクスケジューラ等）では `$false` となり上書きをスキップする。Unix の `curl | sh` とは異なり、`irm | iex` はパイプ経由でも非インタラクティブにはならない点に注意。

| 条件 | 挙動 |
|---|---|
| 既存バイナリなし | 確認なしにインストール |
| `-Force` 指定あり | 確認なしに上書き |
| 既存バイナリあり + インタラクティブ | `y/N` 確認プロンプト（デフォルト拒否）|
| 既存バイナリあり + 非インタラクティブ | 上書きをスキップし `-Force` 使用方法を案内 |

### 終了コード

| Code | 意味 |
|---|---|
| `0` | 成功 |
| `1` | エラー |

### 標準出力（正常系）

```
Fetching latest version... v1.2.3
Downloading wtm v1.2.3 for windows-x64...
Verifying SHA256 checksum...
Installing to C:\Users\user\AppData\Local\Programs\wtm\wtm.exe...

✓ wtm v1.2.3 installed successfully!

Next steps:
  wtm --version
  wtm --help
```

### PATH未登録時の案内

```
Note: C:\Users\user\AppData\Local\Programs\wtm is not in your PATH.
To add it permanently, run in an Administrator PowerShell:
  [Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";C:\Users\user\AppData\Local\Programs\wtm", "User")
Then restart your terminal.
```

---

## 3. GitHub Pages 配信仕様

### 配信ファイル

| ファイル | 安定URL |
|---|---|
| `scripts/install.sh` | `https://kuju63.github.io/wt/install.sh` |
| `scripts/install.ps1` | `https://kuju63.github.io/wt/install.ps1` |

### デプロイ方法

`release.yml` の `build-and-deploy-docs` ジョブに以下ステップを追加：

```yaml
- name: Copy install scripts to site
  run: |
    cp scripts/install.sh _site/install.sh
    cp scripts/install.ps1 _site/install.ps1
```

このステップは "Build documentation" の後、"Deploy to GitHub Pages" の前に実行する。

---

## 4. リリース資産への追加仕様

`release.yml` の `Create GitHub Release` ステップの `files:` に以下を追加：

```yaml
files: |
  ...（既存）...
  scripts/install.sh
  scripts/install.ps1
```

これにより `https://github.com/kuju63/wt/releases/download/v1.2.3/install.sh` としてもアクセス可能になる（安定URLとは別に各バージョン毎にもアーカイブされる）。

---

## 5. README.md 更新仕様

### 追加セクション位置

既存の `### Download from Releases (Recommended)` の前に `### Quick Install (Recommended)` セクションを追加。

### 内容

```markdown
### Quick Install (推奨)

**macOS / Linux:**

\```bash
curl -fsSL https://kuju63.github.io/wt/install.sh | sh
\```

**Windows (PowerShell):**

\```powershell
irm https://kuju63.github.io/wt/install.ps1 | iex
\```

インストールスクリプトは自動的に：
- プラットフォーム・アーキテクチャを検出
- 最新バージョンをダウンロード
- SHA256チェックサムを検証
- `~/.local/bin`（Unix）または `%LOCALAPPDATA%\Programs\wtm`（Windows）にインストール
```
