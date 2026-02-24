# Implementation Plan: インストール簡素化

**Branch**: `007-simplify-install` | **Date**: 2026-02-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/007-simplify-install/spec.md`

## Summary

インストールスクリプト（`scripts/install.sh` / `scripts/install.ps1`）を新規作成し、GitHub Pages および GitHub Releases 経由で安定URLから配布することで、ワンライナーインストールを実現する。C# コードの変更はなし。プラットフォーム自動検出・SHA256検証・PATH案内・TTY対応の上書き確認を一括で担うシェルスクリプトと、それを配信するためのワークフロー・ドキュメント変更が主な実装対象。

## Technical Context

**Language/Version**: Bash (POSIX sh 互換) / PowerShell 5.1+
**Primary Dependencies**: `curl` または `wget` (Unix)、`Invoke-WebRequest` (Windows PowerShell 組み込み)
**Storage**: N/A（ファイルシステムへのバイナリ配置のみ）
**Testing**: `bash -n` 構文検査、`shellcheck` 静的解析、GitHub Actions CI
**Target Platform**: Linux (x64/ARM)、macOS (ARM64)、Windows (x64)
**Project Type**: CLI ツール向けインストールスクリプト（単一プロジェクト）
**Performance Goals**: インストール完了まで1分以内（SC-001）
**Constraints**: `jq` / `python3` などの追加依存なし、sudo不要なユーザースコープインストール
**Scale/Scope**: 単一機能追加（シェルスクリプト2ファイル + ワークフロー変更 + ドキュメント更新）

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Developer Usability | ✅ PASS | ワンライナーインストール、明確なエラーガイダンス、PATH案内を実装 |
| II. Cross-Platform | ✅ PASS | Unix (.sh) と Windows (.ps1) を対称的に実装。各プラットフォームでCIテスト |
| III. Clean & Secure Code | ✅ PASS | SHA256検証必須、最小権限（sudo不要）、`shellcheck` 静的解析でセキュリティチェック |
| IV. Documentation Clarity | ✅ PASS | README.md に日本語/英語両方のインストールコマンドをインライン記載 |
| V. Minimal Dependencies | ✅ PASS | `curl`/`wget` のみ（標準ツール）。`jq` 不使用、`grep`+`sed` で代替 |
| VI. Comprehensive Testing | ✅ PASS | `bash -n` 構文検査 + `shellcheck` + GitHub Actions 統合テスト |
| VII. Quantitative Thresholds | ✅ PASS | スクリプトは短小（各50行以内目標）、循環的複雑度は低い |

**Post-Design Re-check**: FR-016/017/018（TTY/force/非インタラクティブ）追加後も全ゲートをパス。TTY検出ロジック（`[ -t 0 ]`）は POSIX 準拠で Principle II/III を満たす。

**Complexity Tracking**: 該当なし（憲章違反なし）

## Project Structure

### Documentation (this feature)

```text
specs/007-simplify-install/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── install-script-interface.md  # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
scripts/                          # 新規ディレクトリ
├── install.sh                    # Unix用インストールスクリプト (新規)
└── install.ps1                   # Windows PowerShell用インストールスクリプト (新規)

.github/
└── workflows/
    └── release.yml               # 変更: リリース資産追加 + GitHub Pages配信

docs/
└── installation.md               # 変更: クイックインストールセクション追加

README.md                         # 変更: Quick Install セクション追加
```

**Structure Decision**: 単一プロジェクト構造（Option 1）。C# ソースコードの変更なし。`scripts/` ディレクトリを新規作成してインストールスクリプトを管理する。既存の `.github/scripts/` はビルド/リリース自動化スクリプトを格納しているため、ユーザー向けインストールスクリプトは分離して `scripts/` に配置する。

## Implementation Phases

### Phase 1: インストールスクリプト作成

1. `scripts/install.sh` 作成
   - プラットフォーム/アーキテクチャ検出（`uname -s` / `uname -m`）
   - GitHub API で最新バージョン取得（`grep`+`sed`、`jq` 不使用）
   - 既存インストール確認（同一バージョンならスキップ）
   - **TTY/force/非インタラクティブ上書き制御**（FR-016/017/018）:
     - `[ -t 0 ]` で TTY 判定
     - `--force` フラグで確認スキップ
     - 非TTY環境では上書きスキップ＋`--force` 使用案内
   - インストール先ディレクトリ作成（存在しない場合）
   - バイナリダウンロード（`curl` → `wget` フォールバック）
   - SHA256検証（`sha256sum` → `shasum` フォールバック）
   - `chmod +x` で実行権限付与
   - PATH確認・シェル設定ファイルへの追記コマンド案内
   - macOS Gatekeeper 案内

2. `scripts/install.ps1` 作成（Unix版と対称的な機能セット）
   - `[Environment]::UserInteractive` でインタラクティブ判定（FR-016/018）
   - `-Force` パラメータ対応（FR-017）

### Phase 2: ワークフロー変更

`release.yml` に以下を追加：
1. `create-release` ジョブ: `Create GitHub Release` ステップの `files:` に `scripts/install.sh` / `scripts/install.ps1` を追加
2. `build-and-deploy-docs` ジョブ: "Build documentation" と "Deploy to GitHub Pages" の間に `cp scripts/install.sh _site/install.sh` / `cp scripts/install.ps1 _site/install.ps1` ステップを追加

### Phase 3: ドキュメント更新

1. `README.md`: `### Download from Releases (Recommended)` の前に `### Quick Install (推奨)` セクション追加
2. `docs/installation.md`: 冒頭に「クイックインストール」セクション追加（既存手順を保持）

### Phase 4: CI テスト追加

`test.yml` または新規ワークフローに `shellcheck` 静的解析ステップを追加

## Key Decisions

| 決定事項 | 根拠 |
|----------|------|
| GitHub Pages で安定URL配信 | バージョン非依存の永続URL実現（SC-005）。DocFX `resource` 追加より確実 |
| `jq` 不使用、`grep`+`sed` で代替 | 最小依存の原則（Principle V） |
| `[ -t 0 ]` で TTY 判定 | POSIX 準拠、bash/dash/sh 全環境で動作 |
| `--force` フラグ受け渡しは `sh -s --` 構文 | `curl \| sh` パターンでのパラメータ渡しの標準的手法 |
| `~/.local/bin` をデフォルトインストール先 | sudo不要（SC-003）、XDG Base Directory 準拠 |

## Artifacts Generated

| ファイル | 状態 |
|----------|------|
| `specs/007-simplify-install/research.md` | ✅ 完成（セクション11: TTY/force追加済み） |
| `specs/007-simplify-install/data-model.md` | ✅ 完成（--force フラグ、更新済み状態遷移図） |
| `specs/007-simplify-install/quickstart.md` | ✅ 完成 |
| `specs/007-simplify-install/contracts/install-script-interface.md` | ✅ 完成（--force, TTY挙動追加済み） |
| `specs/007-simplify-install/plan.md` | ✅ このファイル |
