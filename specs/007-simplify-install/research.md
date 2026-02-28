# Research: インストール簡素化 (007-simplify-install)

## Phase 0 Research Findings

---

### 1. GitHub Pages 安定URL配信方式

**Decision**: リリースワークフローの `build-and-deploy-docs` ジョブにスクリプトコピーステップを追加し、`_site/` に配置してデプロイする

**Rationale**:

- `docfx.json` の `build.resource` に `scripts/**` を追加する代替案もあるが、DocFXはコード以外のファイルを意図的に変換してしまうリスクがある
- ワークフロー内で `cp scripts/install.sh _site/install.sh` を実行する方が確実かつシンプル
- 既存の `peaceiris/actions-gh-pages@v4` デプロイステップ（`keep_files: true`）との親和性が高い
- 結果URL: `https://kuju63.github.io/wt/install.sh` / `https://kuju63.github.io/wt/install.ps1`

**Alternatives considered**:

- DocFX resourceとして追加: ビルド成果物に含まれるが、DocFXがファイルを解析しようとするリスク
- 独立したGitHub Pagesワークフロー: 複雑さが増すため却下
- GitHub Releases資産のみで配信: バージョン非依存の安定URLが実現できないため却下

---

### 2. バイナリ命名パターン（release.ymlより確認済み）

**Decision**: 以下の命名パターンを採用（バイナリ名 `wtm` に変更後）

| Platform | File Name |
|----------|-----------|
| Linux x64 | `wtm-${VERSION}-linux-x64` |
| Linux ARM | `wtm-${VERSION}-linux-arm` |
| macOS ARM64 | `wtm-${VERSION}-macos-arm64` |
| Windows x64 | `wtm-${VERSION}-windows-x64.exe` |

SHA256ファイル: `wtm-${VERSION}-${PLATFORM}-${ARCH}.sha256`
統合チェックサム: `SHA256SUMS`

---

### 3. プラットフォーム・アーキテクチャ自動検出

**Decision**: `uname -s` / `uname -m` ベースの検出を採用（Unix）

```bash
OS=$(uname -s | tr '[:upper:]' '[:lower:]')
ARCH=$(uname -m)

case "$ARCH" in
  x86_64|amd64) ARCH_TAG="x64" ;;
  aarch64|arm64) ARCH_TAG="arm64" ;;  # macOS ARM64 / Linux ARM64
  armv7l|armhf|arm*)  ARCH_TAG="arm" ;;  # Linux ARM
  *) error_exit "Unsupported architecture: $ARCH" ;;
esac

case "$OS" in
  linux) PLATFORM_TAG="linux" ;;
  darwin) PLATFORM_TAG="macos" ;;
  *) error_exit "Unsupported OS: $OS. See supported platforms." ;;
esac
```

**重要**: macOS ARM64は `uname -m` で `arm64` を返す（`aarch64` ではない）。

**Alternatives considered**:

- `$OSTYPE` 変数: bash限定であり、dash/sh環境では使用不可
- `/etc/os-release`: ディストリビューション検出には有用だが、バイナリ選択には不要

---

### 4. 最新バージョン取得 (GitHub Releases API)

**Decision**: GitHub API の `releases/latest` エンドポイントを使用

```bash
LATEST_VERSION=$(curl -fsSL \
  "https://api.github.com/repos/kuju63/wt/releases/latest" \
  | grep '"tag_name"' \
  | sed -E 's/.*"([^"]+)".*/\1/')
```

**Rationale**: `jq` を依存関係に追加せず、`grep` + `sed` で実装することで標準ツールのみに依存

**Alternatives considered**:

- `jq` を使用: 追加依存が発生するため却下（最小依存の原則）
- `python3 -c`: 利用可能な場合は代替手段として使用可能だが標準化が困難

---

### 5. SHA256検証方式

**Decision**: プラットフォーム別に `sha256sum` (Linux) / `shasum` (macOS) を自動選択

```bash
if command -v sha256sum >/dev/null 2>&1; then
  HASHER="sha256sum"
elif command -v shasum >/dev/null 2>&1; then
  HASHER="shasum -a 256"
else
  error_exit "No SHA256 tool found (sha256sum or shasum)"
fi

# 検証: ダウンロードしたバイナリと.sha256ファイルを比較
EXPECTED=$(curl -fsSL "${HASH_URL}" | cut -d' ' -f1)
ACTUAL=$($HASHER "$BIN_PATH" | cut -d' ' -f1)
if [ "$EXPECTED" != "$ACTUAL" ]; then
  rm -f "$BIN_PATH"
  error_exit "SHA256 verification failed. File deleted. Please retry."
fi
```

---

### 6. macOS Gatekeeper 対応

**Decision**: インストール完了後に `xattr -d com.apple.quarantine` コマンドを案内メッセージに含める

```bash
if [ "$PLATFORM_TAG" = "macos" ]; then
  echo "Note: If you encounter 'cannot be opened because the developer cannot be verified',"
  echo "run: xattr -d com.apple.quarantine ${INSTALL_PATH}"
fi
```

**Rationale**: Gatekeeperブロックは自動解除できない（セキュリティリスク）。ユーザーへの手順案内のみ行う。

---

### 7. インストール先ディレクトリ

**Decision**:

- Unix デフォルト: `~/.local/bin` (sudo不要)
- Windows デフォルト: `$env:LOCALAPPDATA\Programs\wtm`
- `--prefix DIR` オプションでカスタマイズ可能

**PATH案内**:

```bash
if ! echo "$PATH" | tr ':' '\n' | grep -qx "$INSTALL_DIR"; then
  echo "Add to PATH: export PATH=\"${INSTALL_DIR}:\$PATH\""
  echo "Add the above to ~/.bashrc, ~/.zshrc, or ~/.profile"
fi
```

---

### 8. curl / wget フォールバック

**Decision**: `curl` を優先、なければ `wget` にフォールバック

```bash
download() {
  local url="$1" dest="$2"
  if command -v curl >/dev/null 2>&1; then
    curl -fsSL -o "$dest" "$url"
  elif command -v wget >/dev/null 2>&1; then
    wget -q -O "$dest" "$url"
  else
    error_exit "Neither curl nor wget found. Please download manually: $url"
  fi
}
```

---

### 9. アップデート検出（既存インストール確認）

**Decision**: 既存バイナリのバージョン確認 → 同一なら skip、新しければ上書き

```bash
if command -v "$INSTALL_PATH" >/dev/null 2>&1; then
  CURRENT=$("$INSTALL_PATH" --version 2>/dev/null | head -1 || echo "unknown")
  if [ "$CURRENT" = "$LATEST_VERSION" ]; then
    echo "wtm ${LATEST_VERSION} is already installed and up to date."
    exit 0
  fi
  echo "Updating wtm from $CURRENT to $LATEST_VERSION..."
fi
```

---

### 10. リリースワークフローへのスクリプト追加

**Decision**: `release.yml` の `Create GitHub Release` ステップの `files:` に `scripts/install.sh` と `scripts/install.ps1` を追加

これにより各バージョンのリリース資産にもインストールスクリプトが含まれる。安定URLはGitHub Pages経由で提供する。

---

### 11. インタラクティブ環境検出・上書き確認 (FR-016/017/018)

**Decision**: `[ -t 0 ]`（stdin が TTY かどうか）を使用してインタラクティブ環境を判定し、`--force` フラグで強制上書きを制御する

```bash
# インタラクティブ環境（TTY）の判定
is_interactive() {
  [ -t 0 ]
}

# 既存バイナリ存在時の上書き制御ロジック
handle_existing_install() {
  local install_path="$1"
  if [ ! -f "$install_path" ]; then
    return 0  # 新規インストール: 確認不要
  fi

  if [ "$FORCE" = "true" ]; then
    return 0  # --force: 確認スキップ
  fi

  if ! is_interactive; then
    # 非インタラクティブ（パイプ経由等）: スキップして案内
    echo "Note: $install_path already exists."
    echo "Re-run with --force to overwrite: curl ... | sh -s -- --force"
    exit 0
  fi

  # インタラクティブ: y/N プロンプト（デフォルト拒否）
  printf "Overwrite existing %s? [y/N] " "$install_path"
  read -r answer < /dev/tty
  case "$answer" in
    [yY]) return 0 ;;
    *) echo "Installation cancelled."; exit 0 ;;
  esac
}
```

**重要**: パイプ実行（`curl | sh`）では stdin が TTY でないため `[ -t 0 ]` は `false` → 非インタラクティブパスに入る。これが FR-018 の要件（パイプ実行では確認なしにスキップ）を自然に満たす。

**`--force` フラグの受け渡し**: `curl | sh -s -- --force` のように `--` の後に渡す。

**Alternatives considered**:

- `tty` コマンド: POSIX標準だが `[ -t 0 ]` より可搬性が若干低い
- `/dev/tty` から読み込む: TTY判定なしに stdin を TTY に切り替える方法だが、存在しない環境でエラー
