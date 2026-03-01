# Quickstart: インストール簡素化 実装ガイド (007-simplify-install)

## 概要

このフィーチャーは C# コードの変更なし。以下のファイルを新規作成・変更する。

---

## 作成するファイル

### 1. `scripts/install.sh` (新規)

Unix用インストールスクリプト。以下の処理を順番に実行する：

1. プラットフォーム/アーキテクチャ検出 (`uname -s` / `uname -m`)
2. GitHub API で最新バージョン取得
3. 既存インストール確認 → 同一バージョンならスキップ
4. インストール先ディレクトリ作成
5. バイナリダウンロード (curl → wget フォールバック)
6. SHA256検証 (sha256sum → shasum フォールバック)
7. `chmod +x` で実行権限付与
8. PATH確認・案内
9. macOS Gatekeeper 案内 (macOSのみ)

### 2. `scripts/install.ps1` (新規)

Windows PowerShell用インストールスクリプト。Unix版と対称的な機能セット。

---

## 変更するファイル

### 3. `.github/workflows/release.yml`

**変更1**: `create-release` ジョブの `Create GitHub Release` ステップに `scripts/install.sh` と `scripts/install.ps1` を追加

**変更2**: `build-and-deploy-docs` ジョブに「インストールスクリプトを `_site/` へコピー」ステップを追加（"Build documentation" と "Deploy to GitHub Pages" の間）

### 4. `README.md`

`### Download from Releases (Recommended)` の前に `### Quick Install (推奨)` セクションを追加。

### 5. `docs/installation.md`

既存の手動インストール手順を保持したまま、冒頭に「クイックインストール」セクションを追加。

---

## テスト計画

| テスト項目 | 方法 |
|---|---|
| `install.sh` の構文検査 | `bash -n scripts/install.sh` |
| `install.ps1` の構文検査 | `powershell -File scripts/install.ps1 -WhatIf` (オプション) |
| CI での実行テスト | `test.yml` にシェルスクリプト静的解析ステップ追加 (shellcheck) |
| GitHub Pages URL検証 | リリース後 curl でスクリプトが返ることを確認 |

---

## 実装順序（推奨）

1. `scripts/install.sh` 作成 → ローカルで動作確認
2. `scripts/install.ps1` 作成
3. `release.yml` の `create-release` ジョブ更新（リリース資産追加）
4. `release.yml` の `build-and-deploy-docs` ジョブ更新（Pages配信）
5. `README.md` 更新
6. `docs/installation.md` 更新

---

## 依存関係確認

- **006-release-hash-files** が実装済みであること:
  - `SHA256SUMS` と `.sha256` 個別ファイルがリリース資産に含まれている ✅ (確認済み)
  - `.github/scripts/generate-checksums.sh` が存在する ✅ (確認済み)

---

## 安定URL

| 用途 | URL |
|---|---|
| Unix インストール | `https://kuju63.github.io/wt/install.sh` |
| Windows インストール | `https://kuju63.github.io/wt/install.ps1` |

これらのURLはバージョンに依存しない永続URL。スクリプト内容を更新しても同じURLでアクセス可能。
