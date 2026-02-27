# CLI Command Rename (wt → New Unique Name)

**Feature Branch**: `008-rename-command`
**Created**: `2026-02-25`
**Status**: Draft

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Windows ユーザーのコマンド競合解消 (Priority: P1)

`wt` コマンドが Windows Terminal のコマンドと同名であるため、Windows 環境ではワークツリー管理ツールが起動できない。コマンド名を一意な名前に変更することで、この競合を解消する。

**Why this priority**: Windows ユーザーがツールをまったく使用できないという根本的な問題であり、最優先で解決すべきである。

**Independent Test**: Windows Terminal がインストールされた環境で新コマンド名を実行し、ワークツリー管理ツールが起動することを確認することで独立してテスト可能。

**Acceptance Scenarios**:

1. **Given** Windows Terminal がインストールされている環境, **When** 新しいコマンド名（例: `wtm`）を実行する, **Then** Windows Terminal ではなくワークツリー管理ツールが起動する
2. **Given** 旧バイナリ（`wt`）がインストールされた環境, **When** 新バージョンをインストールする, **Then** 新コマンド名でワークツリー管理ツールが使用できる

---

### User Story 2 - コマンド名の一意性検証 (Priority: P2)

新コマンド名が Homebrew・winget および Windows・macOS・Linux の組み込みコマンドと競合しないことを確認し、すべてのプラットフォームで安全に使用できるコマンド名を選定する。

**Why this priority**: コマンド名の一意性検証を経て初めて、安全に名前変更を実施できる。US-001 の前提条件となる調査タスク。

**Independent Test**: `wtm` を Homebrew・winget で検索し、各OS の組み込みコマンドリストと照合することで独立して検証可能。

**Acceptance Scenarios**:

1. **Given** `wtm` を候補名とする, **When** Homebrew および winget で `wtm` を検索する, **Then** 競合するパッケージが存在しない
2. **Given** 候補名が既存パッケージと競合する, **When** 一意性検証を実施する, **Then** 候補名を却下し、代替名を選定する
3. **Given** `wtm` を候補名とする, **When** Windows・macOS・Linux の組み込み・標準配布コマンドと照合する, **Then** いずれのプラットフォームにおいても競合が存在しない

---

### Edge Cases

- 旧 `wt` と新コマンドバイナリが同一マシンに共存する場合、PATH の優先順位によって意図しないコマンドが実行される可能性がある

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: コマンド名を `wt` から新しい一意の名前に変更する
- **FR-002**: 新コマンド名が Homebrew に未登録であること
- **FR-003**: 新コマンド名が winget に未登録であること
- **FR-004**: 新コマンド名が Windows・macOS・Linux の組み込みコマンドと競合しないこと
- **FR-005**: ユーザー向けドキュメントをすべて新コマンド名で更新する
- **FR-006**: ビルド・パッケージング設定を新コマンド名で更新する
- **FR-007**: 既存機能をすべて維持する（機能退行なし）
- **FR-008**: CI/CD・リリーススクリプト内の旧コマンド名参照を更新する

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Windows 環境で新コマンド名実行時に Windows Terminal が起動しない
- **SC-002**: 採用時点で Homebrew・winget に競合パッケージが存在しない
- **SC-003**: ユーザー向けドキュメント内の旧コマンド名参照が 100% 置換済みである
- **SC-004**: 既存 CLI 機能すべてが新コマンド名で動作し、機能退行がゼロである

## Assumptions

- 新名の有力候補は `wtm`（Worktree Manager）だが、採用前に一意性検証を行う
- 後方互換性エイリアス（`wt` → 新名）は不要（Breaking Change として扱う）
- リポジトリ名（`wt`）は変更しない
- dotnet ツール ID およびパッケージ名も新コマンド名に合わせて更新する

## Dependencies

- Homebrew フォーミュラ・winget マニフェスト（存在する場合）の更新
- CI/CD ワークフロー、GitHub Actions、リリーススクリプトの更新
