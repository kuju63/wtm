# Tasks: インストール簡素化

**Input**: Design documents from `/specs/007-simplify-install/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: `shellcheck` 静的解析 + `bash -n` 構文検査を CI に追加（シェルスクリプト向け）。ユニットテストフレームワークは不使用。

**Organization**: ユーザーストーリー別に整理。各フェーズが独立してテスト可能なインクリメントを形成。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 並列実行可（異なるファイル、未完了タスクへの依存なし）
- **[Story]**: 対応するユーザーストーリー (US1〜US6)
- 各タスクに実際のファイルパスを明記

---

## Phase 1: Setup（共有インフラ）

**Purpose**: リポジトリ構造の初期化と CI 環境の整備

- [ ] T001 `scripts/` ディレクトリをリポジトリルートに作成（`mkdir scripts/`）
- [ ] T002 `.github/workflows/test.yml` に `shellcheck` 静的解析ステップを追加する。`scripts/install.sh` と `scripts/install.ps1` を対象とする。スクリプトが存在しない段階での CI 失敗を防ぐため、ステップに `if: hashFiles('scripts/install.sh') != ''` 条件を付与すること。このステップは PR トリガー（`pull_request: branches: [main]`）で動作するため、`007-simplify-install` ブランチからの PR 作成後に shellcheck が実行されることを確認する

---

## Phase 2: Foundational（スクリプト骨格）

**Purpose**: US1〜US6 の全ユーザーストーリーが依存するスクリプト骨格を作成する

**⚠️ CRITICAL**: このフェーズ完了前に US フェーズの実装を開始しないこと

- [ ] T003 `scripts/install.sh` の骨格を作成する。内容: `#!/bin/sh` shebang、`set -e`、`error_exit()` 関数（メッセージを stderr に出力して exit 1）、`download()` 関数（curl → wget フォールバック）、引数ループ（`--prefix` で `INSTALL_DIR` を設定、`--force` で `FORCE=true` を設定）、変数初期化（`INSTALL_DIR="${HOME}/.local/bin"` / `FORCE=false`）、メイン処理のプレースホルダーコメント
- [ ] T004 [P] `scripts/install.ps1` の骨格を作成する。内容: `param([string]$Prefix = "$env:LOCALAPPDATA\Programs\wt", [switch]$Force)` ブロック、`Exit-WithError` 関数（メッセージを stderr に出力して exit 1）、`Invoke-Download` 関数（`Invoke-WebRequest -UseBasicParsing`）、変数初期化（`$InstallDir = $Prefix`）、メイン処理のプレースホルダーコメント

**Checkpoint**: 両スクリプトの骨格が `bash -n scripts/install.sh` および `shellcheck scripts/install.sh` をパスすること

---

## Phase 3: User Story 1 - ワンライナーでのインストール完了（Priority: P1）🎯 MVP

**Goal**: macOS/Linux/Windows 環境でターミナルに1行コマンドを実行するだけで `wt --version` が動作する状態にする

**Independent Test**: `bash scripts/install.sh` を実行して `~/.local/bin/wt` にバイナリが配置され `wt --version` が成功すること

### Implementation for User Story 1

- [ ] T005 [US1] `scripts/install.sh` にプラットフォーム/アーキテクチャ自動検出を実装する。`uname -s`→`PLATFORM_TAG`（linux/macos）、`uname -m`→`ARCH_TAG`（x64/arm64/arm）のマッピング、非対応時は `error_exit` でサポート済みプラットフォーム一覧と `https://github.com/kuju63/wt/releases` を案内
- [ ] T006 [US1] `scripts/install.sh` に GitHub API 最新バージョン取得を実装する。`https://api.github.com/repos/kuju63/wt/releases/latest` を `download()` 関数で取得し、`grep '"tag_name"' | sed -E 's/.*"([^"]+)".*/\1/'` でバージョン文字列を抽出して `LATEST_VERSION` に格納
- [ ] T007 [US1] `scripts/install.sh` にバイナリダウンロード + SHA256検証を実装する。`BINARY_NAME="wt-${LATEST_VERSION}-${PLATFORM_TAG}-${ARCH_TAG}"`、`DOWNLOAD_URL` と `HASH_URL` を構築し、バイナリをダウンロード後に `sha256sum`→`shasum -a 256` フォールバックで検証、失敗時はバイナリを削除して `error_exit`
- [ ] T008 [US1] `scripts/install.sh` にインストール先ディレクトリ作成・バイナリ配置・実行権限付与を実装する。`mkdir -p "$INSTALL_DIR"`、バイナリを `"${INSTALL_DIR}/wt"` に移動、`chmod +x "${INSTALL_DIR}/wt"`
- [ ] T009 [US1] `scripts/install.sh` に PATH 未登録時の案内を実装する。`echo "$PATH" | tr ':' '\n' | grep -qx "$INSTALL_DIR"` で判定、未登録の場合は `export PATH=...` コマンドと `~/.bashrc`/`~/.zshrc` への追記方法を出力
- [ ] T010 [US1] `scripts/install.sh` に macOS Gatekeeper 案内とインストール完了メッセージを実装する。`[ "$PLATFORM_TAG" = "macos" ]` が真の場合のみ `xattr -d com.apple.quarantine "${INSTALL_DIR}/wt"` を案内。最後に `✓ wt ${LATEST_VERSION} installed successfully!` + `wt --version` / `wt --help` の次のステップ案内を出力
- [ ] T011 [P] [US1] `scripts/install.ps1` にバージョン取得・Windows バイナリ名構築を実装する（Part 1/3）。`Invoke-RestMethod "https://api.github.com/repos/kuju63/wt/releases/latest"` で `$LatestVersion` を取得（失敗時は `Exit-WithError` でレート制限・ネットワークエラーを案内）、`$BinaryName = "wt-${LatestVersion}-windows-x64.exe"`・`$DownloadUrl`・`$HashUrl` を構築
- [ ] T011b [P] [US1] `scripts/install.ps1` にバイナリダウンロード + SHA256検証を実装する（Part 2/3）。`Invoke-Download` でバイナリをダウンロード（失敗時は `Exit-WithError` でネットワーク接続確認を案内）、`Get-FileHash -Algorithm SHA256` でハッシュを取得し `.sha256` ファイルの値と比較、不一致時はバイナリを削除して `Exit-WithError`
- [ ] T011c [P] [US1] `scripts/install.ps1` にディレクトリ作成・バイナリ配置・PATH案内・完了メッセージを実装する（Part 3/3）。`New-Item -ItemType Directory -Force`、バイナリを `${InstallDir}\wt.exe` にコピー、`[Environment]::GetEnvironmentVariable("PATH", "User")` で PATH 確認・未登録時に `[Environment]::SetEnvironmentVariable(...)` コマンドを案内、`✓ wt ${LatestVersion} installed successfully!` を出力
- [ ] T012 [US1] `.github/workflows/release.yml` の `create-release` ジョブ内 `Create GitHub Release` ステップの `files:` セクション（line 438付近）に `scripts/install.sh` と `scripts/install.ps1` を追加
- [ ] T013 [US1] `.github/workflows/release.yml` の `build-and-deploy-docs` ジョブ内「Build documentation」ステップと「Deploy to GitHub Pages」ステップの間に新規ステップ `Copy install scripts to site` を追加する。内容: `cp scripts/install.sh _site/install.sh` と `cp scripts/install.ps1 _site/install.ps1`

**Checkpoint**: `bash scripts/install.sh` を実行して `~/.local/bin/wt` にバイナリが配置され `wt --version` が成功すること

---

## Phase 4: User Story 2 - README.mdだけでインストール完了（Priority: P1）

**Goal**: GitHub の `kuju63/wt` リポジトリトップページの README.md だけを読めば、外部リンクなしにインストールコマンドが取得できる状態にする

**Independent Test**: `README.md` の Installation セクションを読んで外部リンクなしに macOS/Linux/Windows 向けのコマンドが確認できること

### Implementation for User Story 2

- [ ] T014 [P] [US2] `README.md` の `## Installation` セクション内、`### Download from Releases (Recommended)`（line 23付近）の直前に `### Quick Install (推奨)` セクションを追加する。macOS/Linux 向け `curl -fsSL https://kuju63.github.io/wt/install.sh | sh` と Windows 向け `irm https://kuju63.github.io/wt/install.ps1 | iex` のコードブロック、および自動実行内容の箇条書き（プラットフォーム検出・ダウンロード・SHA256検証・インストール）を含める
- [ ] T015 [P] [US2] `docs/installation.md` の冒頭（既存の手動インストール手順の前）に「クイックインストール」セクションを追加する。macOS/Linux と Windows の1行コマンドをコードブロックで記載し、**既存の手動インストール手順は変更しない**（SC-007: 既存ユーザーへの影響ゼロを保証するため、既存セクションの削除・移動を行わないこと）

**Checkpoint**: `README.md` を読むだけで（外部リンクを踏まずに）macOS/Linux/Windows の各インストールコマンドが確認できること

---

## Phase 5: User Story 3 - ルート権限不要なユーザースコープインストール（Priority: P2）

**Goal**: `--prefix DIR` / `-Prefix <path>` オプションでインストール先を指定できる。デフォルトは sudo 不要なユーザースコープディレクトリ

**Independent Test**: `bash scripts/install.sh --prefix /tmp/test-install` を実行して `/tmp/test-install/wt` にバイナリが配置されること

### Implementation for User Story 3

- [ ] T016 [US3] `scripts/install.sh` の引数処理（T003 の骨格ループ）に `--prefix` 実装を確認・補完する。`--prefix` で指定されたパスを `INSTALL_DIR` に設定し、`mkdir -p` で自動作成、指定ディレクトリが PATH 未登録の場合は追記コマンドを案内する（既存の PATH 案内ロジックを再利用）
- [ ] T017 [P] [US3] `scripts/install.ps1` の `-Prefix` パラメータ処理（T004 の骨格）を確認・補完する。`$Prefix` で指定されたパスを `$InstallDir` に設定し、`New-Item -Force` で自動作成、PATH 未登録の場合は `[Environment]::SetEnvironmentVariable(...)` コマンドを案内する

**Checkpoint**: `--prefix` / `-Prefix` でカスタムパスへのインストールが sudo なしで完了すること

---

## Phase 6: User Story 4 - 再実行で自動アップデート（Priority: P2）

**Goal**: インストールスクリプトを再実行するだけで最新版に更新できる。同一バージョンが既インストール済みの場合はスキップ

**Independent Test**: 旧バージョンの wt がインストールされた環境でスクリプトを再実行し、新バージョンが上書きされること（同一バージョン時はスキップメッセージが表示されること）

### Implementation for User Story 4

- [ ] T018 [US4] `scripts/install.sh` に既存インストール確認ロジックを追加する。バージョン取得後、`command -v "${INSTALL_DIR}/wt"` で既存バイナリを確認し、存在する場合は `"${INSTALL_DIR}/wt" --version` でバージョンを取得して `$LATEST_VERSION` と比較。同一バージョンなら `"wt ${LATEST_VERSION} is already the latest version. Skipping."` を出力して exit 0。旧バージョンなら `"Updating wt from ${CURRENT_VERSION} to ${LATEST_VERSION}..."` を表示してインストールフローを続行する。**注意**: この時点では旧バージョン検出後に上書き確認なしでインストールフローに進む暫定実装となる。Phase 7（T020）で `handle_existing_install()` 関数を挿入することで上書き確認が追加される。T020 実装前後で動作が変わることを意識して実装すること
- [ ] T019 [P] [US4] `scripts/install.ps1` に既存インストール確認ロジックを追加する。`Test-Path $InstallPath` で既存バイナリを確認し、存在する場合は `wt.exe --version` でバージョン取得、同一なら skip メッセージを出力して exit 0、旧バージョンなら更新メッセージを表示してフロー続行（T021 で上書き確認が追加される暫定実装）

**Checkpoint**: 同一バージョンインストール済みの環境でスクリプト再実行時にスキップメッセージが表示され、既存バイナリが変更されないこと

---

## Phase 7: User Story 6 - 既存バイナリの上書き確認（Priority: P2）

**Goal**: 既存バイナリが存在する場合に TTY 環境では `y/N` 確認プロンプトを表示。`--force` 指定時はスキップ。非インタラクティブ環境では上書きをスキップして案内メッセージを表示

**Independent Test**: インストール先に既存の wt バイナリが存在する状態でスクリプトを実行し、TTY 環境では `y/N` プロンプトが表示され、`--force` 指定時はプロンプトなしで上書きされること

### Implementation for User Story 6

- [ ] T020 [US6] `scripts/install.sh` に `handle_existing_install()` 関数を実装し、T018 の「旧バージョン検出後にインストールフロー続行」の直前に呼び出しを挿入する。関数ロジック: (1) ファイルが存在しない→return 0（新規インストール）、(2) `FORCE=true`→return 0（強制上書き）、(3) `[ -t 0 ]` が false（非TTY）→`"Note: ${install_path} already exists. Skipping. Re-run with --force: sh -s -- --force"` を出力して exit 0、(4) TTY: `printf "Overwrite %s? [y/N] "` → `read -r answer < /dev/tty`、`[yY]`→return 0、それ以外→`"Installation cancelled."` → exit 0。この実装により T018 の暫定動作が完全な FR-016/017/018 準拠に更新される
- [ ] T021 [P] [US6] `scripts/install.ps1` に上書き確認ロジックを実装し、T019 の「旧バージョン検出後にフロー続行」の直前に挿入する。`-Force` スイッチが指定されていれば確認なし。`[Environment]::UserInteractive` が false（非インタラクティブ）なら `"Note: $InstallPath already exists. Skipping. Re-run with -Force."` を出力して exit 0。インタラクティブの場合は `Read-Host "Overwrite? [y/N]"` でプロンプト表示、`y` 以外でキャンセル

**Checkpoint**: `curl -fsSL .../install.sh | sh`（非TTY）で既存バイナリが存在する場合にスキップメッセージが表示され、`sh -s -- --force` では上書きされること

---

## Phase 8: User Story 5 - 失敗時の分かりやすいエラーガイダンス（Priority: P3）

**Goal**: ネットワーク不通・非対応プラットフォーム・SHA256検証失敗・GitHub API レート制限などのエラー時に、問題の原因と解決策を示す分かりやすいメッセージを出力する

**Independent Test**: 各エラー条件でスクリプトを実行し、出力メッセージだけで次のアクションが分かること

### Implementation for User Story 5

- [ ] T022 [US5] `scripts/install.sh` の `download()` 関数とバージョン取得処理のエラーパスに分かりやすいメッセージを追加する。(a) curl/wget 両方が存在しない場合: `"Error: Neither curl nor wget found. Install one and retry."` + `"Manual download: https://github.com/kuju63/wt/releases"`。(b) ダウンロード失敗（非ゼロ終了）時: `"Error: Failed to download from ${url}. Check your network connection."`。(c) **GitHub API バージョン取得失敗**（`LATEST_VERSION` が空の場合）: `"Error: Failed to fetch latest version from GitHub API. This may be due to rate limiting. Please wait a moment and retry."` + `"Manual install: https://github.com/kuju63/wt/releases"`
- [ ] T023 [P] [US5] `scripts/install.sh` のプラットフォーム検出エラーパスと SHA256 検証失敗パスのメッセージを充実させる。非対応 OS: `"Error: Unsupported OS: ${OS}. Supported: linux-x64, linux-arm, macos-arm64. Manual install: https://github.com/kuju63/wt/releases"`。SHA256 失敗: `"Error: SHA256 verification failed for ${BINARY_NAME}. Downloaded file removed. Please retry."` + GitHub Releases URL
- [ ] T024 [P] [US5] `scripts/install.ps1` に同等のエラーメッセージを実装する。(a) バージョン取得失敗（API エラー・レート制限）: `"Error: Failed to fetch latest version. This may be due to GitHub API rate limiting. Please wait and retry."`。(b) ダウンロード失敗・SHA256 検証失敗の各エラーパスに問題内容と解決策を示すメッセージを追加（`Exit-WithError` 関数の呼び出し箇所を整備）

**Checkpoint**: 各エラー条件でスクリプトを実行したとき、ユーザーが出力だけで次のアクションを把握できること

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: 全ユーザーストーリーにまたがる品質確認とドキュメント整備

- [ ] T025 [P] `scripts/install.sh` の最終構文検証を実施する。`bash -n scripts/install.sh` と `shellcheck --severity=warning scripts/install.sh` を実行し、全警告・エラーを解消する
- [ ] T026 [P] `scripts/install.ps1` の最終構文検証を実施する。PowerShell の `[System.Management.Automation.Language.Parser]::ParseFile()` または `pwsh -NoProfile -Command "Get-Content scripts/install.ps1 | Out-Null"` で構文エラーがないことを確認する
- [ ] T027 `quickstart.md` のテスト計画チェックリストを実施する。`bash -n scripts/install.sh` ✅、shellcheck ✅、release.yml の変更ファイルパスの整合性を確認（`scripts/install.sh`、`scripts/install.ps1` が存在すること）。また SC-001（インストール1分以内）の手動計測を実施する: `time bash scripts/install.sh` を実行し、所要時間が1分以内であることを確認してその結果をコメントとして記録する

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 依存なし - 即座に開始可能
- **Foundational (Phase 2)**: Phase 1 完了後 - 全 US フェーズをブロック
- **US1 (Phase 3)**: Phase 2 完了後 - 他の US に非依存（MVP）
- **US2 (Phase 4)**: Phase 2 完了後 - US1 と並列可（別ファイル）
- **US3 (Phase 5)**: Phase 3 完了後（骨格の `--prefix` 実装を確認）
- **US4 (Phase 6)**: Phase 3 完了後（既存バイナリの検出フローが必要）
- **US6 (Phase 7)**: Phase 6 完了後（T020/T021 は T018/T019 のコードに挿入される）
- **US5 (Phase 8)**: Phase 3 完了後 - 既存のエラーパスを充実させる作業
- **Polish (Phase 9)**: 全 US フェーズ完了後

### User Story Dependencies

```
Phase 1 → Phase 2 → Phase 3 (US1) ┬→ Phase 4 (US2) [並列可]
                                    ├→ Phase 5 (US3)
                                    ├→ Phase 6 (US4) → Phase 7 (US6)
                                    └→ Phase 8 (US5)
                                              ↓
                                         Phase 9 (Polish)
```

### Within Each User Story

- `install.sh` の実装 → `install.ps1` の実装（対称的に実装、多くが [P] 並列可）
- ワークフロー変更（T012, T013）は US1 スクリプト実装後
- ドキュメント更新（T014, T015）は US2 として独立して並列実施可
- T020/T021（US6）は T018/T019（US4）の実装コードに挿入される点に注意

---

## Parallel Example: User Story 1

```bash
# Phase 3 で並列実行可能なタスク（別ファイル）:
Task: "T005〜T010: scripts/install.sh の各機能実装（順次）"
Task: "T011, T011b, T011c: scripts/install.ps1 の機能実装（Unix実装と並列可）"

# Phase 4 で完全並列:
Task: "T014: README.md 更新"
Task: "T015: docs/installation.md 更新"
```

---

## Implementation Strategy

### MVP First (User Story 1 のみ)

1. Phase 1: Setup（T001, T002）
2. Phase 2: Foundational（T003, T004）
3. Phase 3: US1 全タスク（T005〜T013）
4. **STOP and VALIDATE**: `bash scripts/install.sh` でローカルインストール確認
5. PR を作成してレビュー → マージ → GitHub Pages デプロイ確認

### Incremental Delivery

1. Phase 1 + 2 → 骨格完成
2. Phase 3 (US1) → ワンライナーインストール動作確認 → **MVP デモ可**
3. Phase 4 (US2) → README 更新 → ユーザー向け公開
4. Phase 5 (US3) + Phase 6 (US4) → --prefix + 自動更新
5. Phase 7 (US6) → 上書き確認（セーフガード）
6. Phase 8 (US5) → エラーガイダンス強化
7. Phase 9 → Polish → リリース

### Parallel Team Strategy

複数人で作業する場合:

1. Phase 1 + 2 を全員で完了
2. US1 完了後:
   - 開発者 A: US3 + US4 + US6（`install.sh` 更新）
   - 開発者 B: US2 ドキュメント更新 + US5 エラーガイダンス
   - 開発者 C: US3 + US4 + US6（`install.ps1` 更新、[P] 並列可）

---

## Notes

- [P] タスク = 別ファイル、未完了タスクへの依存なし → 並列実行可
- [Story] ラベルは spec.md のユーザーストーリー番号と対応
- `install.sh` と `install.ps1` の多くのタスクは [P] 並列実行可（対称実装）
- 各フェーズのチェックポイントでユーザーストーリーを独立してテスト可
- シェルスクリプトの文字コードは UTF-8、改行コードは LF（CRLF 禁止）
- `install.sh` の shebang は `#!/bin/sh`（bash 限定の記述を避ける）
- コミットは各タスクまたは論理グループ単位で実施
- T020/T021 は T018/T019 のコードに後から挿入される形で統合される（詳細は Phase 7 を参照）
