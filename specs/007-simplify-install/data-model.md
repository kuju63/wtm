# Data Model: インストール簡素化 (007-simplify-install)

本フィーチャーは C# ソースコードの追加なし。シェルスクリプトとワークフローYAMLのみの実装。

---

## エンティティ / 主要概念

### 1. InstallScript (Unix)

**File**: `scripts/install.sh`

| フィールド/変数 | 型 | 説明 | デフォルト |
|---|---|---|---|
| `INSTALL_DIR` | string | インストール先ディレクトリ | `${HOME}/.local/bin` |
| `PLATFORM_TAG` | string | プラットフォーム識別子 | 自動検出: `linux` / `macos` |
| `ARCH_TAG` | string | アーキテクチャ識別子 | 自動検出: `x64` / `arm64` / `arm` |
| `LATEST_VERSION` | string | 最新バージョンタグ | GitHub API取得 (例: `v1.2.3`) |
| `BINARY_NAME` | string | ダウンロードするバイナリ名 | `wt-${LATEST_VERSION}-${PLATFORM_TAG}-${ARCH_TAG}` |
| `DOWNLOAD_URL` | string | バイナリのダウンロードURL | `https://github.com/kuju63/wt/releases/download/${LATEST_VERSION}/${BINARY_NAME}` |
| `HASH_URL` | string | SHA256ハッシュファイルURL | `${DOWNLOAD_URL}.sha256` |
| `INSTALL_PATH` | string | インストール先フルパス | `${INSTALL_DIR}/wt` |

**コマンドライン引数**:

| オプション | 説明 |
|---|---|
| `--prefix DIR` | インストール先ディレクトリを変更 |
| `--force` | 既存バイナリへの上書き確認プロンプトをスキップ (FR-017) |

**インタラクティブ判定変数**:

| 変数 | 型 | 説明 |
|---|---|---|
| `FORCE` | bool (`true`/`false`) | `--force` フラグの有無 |
| `IS_TTY` | bool | `[ -t 0 ]` の結果（stdin が TTY かどうか）|

---

### 2. InstallScript (Windows)

**File**: `scripts/install.ps1`

| フィールド/変数 | 型 | 説明 | デフォルト |
|---|---|---|---|
| `$InstallDir` | string | インストール先ディレクトリ | `$env:LOCALAPPDATA\Programs\wt` |
| `$LatestVersion` | string | 最新バージョンタグ | GitHub API取得 |
| `$BinaryName` | string | ダウンロードするバイナリ名 | `wt-${LatestVersion}-windows-x64.exe` |
| `$DownloadUrl` | string | ダウンロードURL | `https://github.com/kuju63/wt/releases/download/${LatestVersion}/${BinaryName}` |
| `$HashUrl` | string | SHA256ハッシュURL | `${DownloadUrl}.sha256` |
| `$InstallPath` | string | インストール先フルパス | `${InstallDir}\wt.exe` |

**パラメータ**:

| パラメータ | 説明 |
|---|---|
| `-Prefix <path>` | インストール先ディレクトリを変更 |
| `-Force` | 既存バイナリへの上書き確認プロンプトをスキップ (FR-017) |

---

## 状態遷移

### インストールフロー (Unix)

```
Start
  │
  ▼
プラットフォーム/アーキテクチャ検出
  │ unsupported
  ├──────────────────────────→ Error: 対応プラットフォーム一覧を表示
  │ supported
  ▼
GitHub API: 最新バージョン取得
  │ API失敗
  ├──────────────────────────→ Error: レート制限 or ネットワークエラー案内
  │ 成功
  ▼
既存インストール確認
  │ 同一バージョン検出
  ├──────────────────────────→ "最新版インストール済み" 通知 → Exit 0
  │ 未インストール
  ├──────────────────────────→ インストール先ディレクトリ作成 (存在しない場合) へ
  │ 旧バージョン（既存バイナリあり）
  ▼
既存バイナリ上書き確認 (FR-016/017/018)
  │ --force 指定
  ├──────────────────────────→ インストール先ディレクトリ作成 (存在しない場合) へ
  │ 非インタラクティブ (stdin が TTY でない)
  ├──────────────────────────→ スキップ通知 + --force 使用案内 → Exit 0
  │ インタラクティブ TTY
  ▼
y/N 確認プロンプト (デフォルト拒否)
  │ n または Enter（デフォルト）
  ├──────────────────────────→ "Installation cancelled." → Exit 0
  │ y
  ▼
インストール先ディレクトリ作成 (存在しない場合)
  │
  ▼
バイナリダウンロード (curl または wget)
  │ ダウンロード失敗
  ├──────────────────────────→ Error: ネットワーク接続確認を案内
  │ 成功
  ▼
SHA256ハッシュファイルダウンロード
  │
  ▼
SHA256検証
  │ 検証失敗
  ├──────────────────────────→ バイナリ削除 → Error: 再試行を案内
  │ 検証成功
  ▼
実行権限付与 (chmod +x)
  │
  ▼
PATH確認
  │ PATH未登録
  ├──────────────────────────→ シェル設定ファイルへの追記コマンドを案内
  │ PATH登録済み
  ▼
macOS Gatekeeper 案内 (macOSのみ)
  │
  ▼
インストール完了: "wt ${VERSION} installed successfully"
```

---

## バリデーションルール

| チェック項目 | 条件 | エラー対応 |
|---|---|---|
| OS検出 | `linux` または `macos` のみ | 対応プラットフォーム一覧表示・手動インストール案内 |
| アーキテクチャ検出 | `x64` / `arm64` / `arm` のみ | 対応アーキテクチャ一覧表示 |
| GitHub API応答 | `tag_name` フィールドが空でない | レート制限・ネットワークエラー案内 |
| SHA256検証 | ダウンロードファイルのハッシュが.sha256ファイルと一致 | バイナリ削除・再試行案内 |
| `--prefix` パス | ディレクトリパスとして有効な文字列 | 無効な値の場合エラーで終了 |

---

## 安定URL設計

| URL | 用途 |
|---|---|
| `https://kuju63.github.io/wt/install.sh` | Unix インストールスクリプト (安定URL) |
| `https://kuju63.github.io/wt/install.ps1` | Windows インストールスクリプト (安定URL) |

これらのURLはGitHub Pagesで配信。スクリプト本体に変更があっても URLは変わらない。
