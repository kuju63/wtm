# ADR 0008: Install Script Distribution and Design Decisions

**Status**: Accepted

**Date**: 2026-02-21

## Context

`wt` コマンドのインストール体験を改善するために、ワンライナーインストールスクリプトを作成・配布する。この際、以下の技術的決定を行う必要があった:

1. スクリプトをどのURLで安定配布するか（GitHub Pages vs リリース資産のみ）
2. Unix スクリプトで JSON をパースする際に `jq` を使用するか
3. インタラクティブ/非インタラクティブ環境の判定方法
4. デフォルトインストール先のディレクトリ
5. SHA256検証に使用するコマンド

## Decision

### 1. 安定URL: GitHub Pages 経由での配信

`release.yml` の `build-and-deploy-docs` ジョブに `cp scripts/install.sh _site/install.sh` ステップを追加し、GitHub Pages 経由で永続URLを提供する。

- **配信URL**: `https://kuju63.github.io/wt/install.sh` / `.../install.ps1`
- GitHub Releases 資産としても各バージョンに含める（バージョン固有URLとして）

### 2. `jq` 不使用、`grep` + `sed` で代替

GitHub API レスポンスのパースには標準ツールのみ使用する:

```sh
LATEST_VERSION=$(curl -fsSL "https://api.github.com/repos/kuju63/wt/releases/latest" \
  | grep '"tag_name"' \
  | sed -E 's/.*"([^"]+)".*/\1/')
```

### 3. TTY 判定: `[ -t 0 ]` (POSIX準拠)

インタラクティブ環境の判定に `[ -t 0 ]`（stdin が TTY かどうか）を使用する。

- `curl | sh` 実行時は stdin が TTY でないため非インタラクティブと判定される（FR-018 の要件を自然に満たす）
- `bash`・`dash`・`sh` 全環境で動作

PowerShell では `[Environment]::UserInteractive` を使用する。

### 4. デフォルトインストール先

| プラットフォーム | デフォルトパス |
|---|---|
| Unix | `~/.local/bin` (XDG Base Directory 準拠、sudo 不要) |
| Windows | `$env:LOCALAPPDATA\Programs\wtm` (ユーザースコープ) |

### 5. SHA256検証コマンド

プラットフォーム別に自動選択:

```sh
if command -v sha256sum >/dev/null 2>&1; then
  HASHER="sha256sum"
elif command -v shasum >/dev/null 2>&1; then
  HASHER="shasum -a 256"
fi
```

## Consequences

- スクリプト配布URLはバージョンに依存しない永続URLとなるため、ドキュメントやリンクを更新不要
- `jq` 依存を排除することでインストール前提条件が最小化される（Constitution Principle V 準拠）
- TTY 判定により `curl | sh` パターンで安全なデフォルト動作（上書きスキップ + `--force` 案内）が実現される
- `sha256sum` / `shasum` の両方をサポートすることで Linux および macOS で動作する

## Alternatives Considered

### GitHub Pages の代替: リリース資産のみで配信

- バージョン固有URLしか提供できず SC-005（安定URL）を満たせないため却下

### DocFX resource として追加

- DocFX がファイルを解析しようとするリスクがあり、ワークフロー内でのコピーより不確実なため却下

### `jq` を使用

- 追加依存が発生し Constitution Principle V（最小依存）に反するため却下

### `$OSTYPE` 変数での OS 判定

- bash 限定であり、dash/sh 環境では使用不可のため `uname -s` を採用

## Authors

- Jun Kurihara <lh182051+src@gmail.com>
