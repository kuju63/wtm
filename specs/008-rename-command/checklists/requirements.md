# Requirements Checklist: 008-rename-command

**Feature**: CLI Command Rename (wt → New Unique Name)
**Date**: 2026-02-25

## Spec Quality Checklist

### User Stories

- [x] User Story 1 (P1) は独立してテスト可能である
- [x] User Story 2 (P2) は独立してテスト可能である
- [x] 各 User Story に Acceptance Scenarios が定義されている
- [x] Breaking Change として後方互換性なしであることが明示されている
- [x] 移行サポート（エイリアス等）を提供しないことが明記されている

### Functional Requirements

- [x] FR-001: コマンド名変更（具体的な変更内容）
- [x] FR-002: Homebrew 未登録確認
- [x] FR-003: winget 未登録確認
- [x] FR-004: OS 組み込みコマンドとの非競合確認
- [x] FR-005: ドキュメント更新
- [x] FR-006: ビルド・パッケージング設定更新
- [x] FR-007: 既存機能の維持（退行なし）
- [x] FR-008: CI/CD・リリーススクリプト更新

### Success Criteria

- [x] SC-001: Windows Terminal との非競合（測定可能）
- [x] SC-002: パッケージマネージャー競合なし（測定可能）
- [x] SC-003: ドキュメント 100% 置換（測定可能）
- [x] SC-004: 機能退行ゼロ（測定可能）

### Coverage

- [x] Windows 環境での競合問題が対処されている
- [x] macOS・Linux での動作も考慮されている
- [x] パッケージマネージャー（Homebrew・winget）が対象に含まれている
- [x] CI/CD パイプラインの更新が要件に含まれている
- [x] ドキュメント更新が要件に含まれている

### Assumptions & Dependencies

- [x] 有力候補名（`wtm`）が明記されている
- [x] リポジトリ名は変更しないことが明記されている
- [x] dotnet ツール ID の更新が前提として記録されている
- [x] 外部依存（Homebrew フォーミュラ・winget マニフェスト）が Dependencies に記録されている

### Gaps / Open Questions

- [ ] `wtm` の一意性検証は実施済みか？（spec 作成時点では未実施）
- [ ] dotnet ツール ID の現在の値は何か？（更新対象の特定が必要）
- [ ] Homebrew フォーミュラ・winget マニフェストは実際に存在するか？
