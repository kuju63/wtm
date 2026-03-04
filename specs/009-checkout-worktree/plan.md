# Implementation Plan: Checkout Existing Branch as Worktree

**Branch**: `009-checkout-worktree` | **Date**: 2026-03-04 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/009-checkout-worktree/spec.md`

## Summary

既存のローカルブランチまたはリモートブランチをワークツリーとしてチェックアウトする `checkout` コマンドを追加する。ローカルブランチを優先し、存在しない場合はキャッシュ済みリモートトラッキング参照を検索する。複数リモートに同名ブランチがある場合はインタラクティブプロンプトで選択させる。`create` コマンドと同様のエディタ統合・パス解決・エラーハンドリングを踏襲する。

## Technical Context

**Language/Version**: C# 13 / .NET 10.0
**Primary Dependencies**: System.CommandLine 2.0.3, System.IO.Abstractions 22.1.0
**Storage**: N/A（ファイルシステムへのワークツリーディレクトリ作成のみ）
**Testing**: xUnit 2.9.3, Shouldly 4.3.0, Moq 4.20.72, System.IO.Abstractions.TestingHelpers 22.1.0
**Target Platform**: Windows (win-x64), Linux (linux-x64, linux-arm), macOS (osx-arm64)
**Project Type**: Single CLI project（`wt.cli` / `wt.tests`）
**Performance Goals**: ローカルブランチチェックアウトは5秒以内（SC-001）、リモートフェッチ込みは30秒以内（SC-002）
**Constraints**: 新規外部依存関係なし（インタラクティブプロンプトは標準入力/出力のみ使用）
**Scale/Scope**: 既存コマンド群との一貫性を維持する小規模追加実装

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Developer Usability | ✅ PASS | コマンド名 `checkout` は動詞、引数・出力は既存コマンドと一貫 |
| II. Cross-Platform | ✅ PASS | System.IO.Abstractions使用、プロセス実行は既存ProcessRunner経由 |
| III. Clean & Secure Code | ✅ PASS | 空catchなし、特定例外捕捉、入力検証あり、秘密情報なし |
| IV. Documentation Clarity | ✅ PASS | XML docコメント追加、ADR不要（既存パターンの拡張） |
| V. Minimal Dependencies | ✅ PASS | 新規外部パッケージなし。インタラクティブ選択は標準I/O |
| VI. Comprehensive Testing | ✅ PASS | TDDサイクル遵守、ユニット/統合/E2Eテストを先行 |

**Post-design re-check**: Phase 1 設計後に再評価 → 変更なし（Constitution違反なし）

## Project Structure

### Documentation (this feature)

```text
specs/009-checkout-worktree/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── checkout-command.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
wt.cli/
├── Commands/
│   └── Worktree/
│       ├── CreateCommand.cs         (既存)
│       ├── RemoveCommand.cs         (既存)
│       └── CheckoutCommand.cs       (NEW)
├── Models/
│   ├── CheckoutWorktreeOptions.cs   (NEW)
│   ├── RemoteBranchInfo.cs          (NEW)
│   └── ErrorCodes.cs                (MODIFY: RMxxx コード追加)
├── Services/
│   ├── Worktree/
│   │   ├── IWorktreeService.cs      (MODIFY: CheckoutWorktreeAsync 追加)
│   │   └── WorktreeService.cs       (MODIFY: CheckoutWorktreeAsync 実装)
│   ├── Git/
│   │   ├── IGitService.cs           (MODIFY: GetRemotesAsync, GetRemoteTrackingBranchesAsync,
│   │   │                                     GetBranchUpstreamRemoteAsync, FetchFromRemoteAsync,
│   │   │                                     AddWorktreeFromRemoteAsync 追加)
│   │   └── GitService.cs            (MODIFY: 上記メソッド実装)
│   └── Interaction/
│       ├── IInteractionService.cs   (NEW: インタラクティブ選択抽象化)
│       └── ConsoleInteractionService.cs (NEW: Console実装)
└── Program.cs                       (MODIFY: CheckoutCommand + InteractionService 登録)

wt.tests/
├── Commands/
│   └── Worktree/
│       └── CheckoutCommandTests.cs  (NEW)
├── Services/
│   ├── Worktree/
│   │   └── WorktreeServiceCheckoutTests.cs (NEW)
│   └── Git/
│       └── GitServiceTests.cs       (MODIFY: 新メソッドのテスト追加)
└── Integration/
    └── CheckoutWorktreeE2ETests.cs  (NEW)
```

**Structure Decision**: Option 1（Single project）を採用。既存の `wt.cli` / `wt.tests` 構造に新ファイルを追加するのみ。新規プロジェクトは不要。

**IInteractionService 導入の判断記録**（軽量 ADR / Constitution IV 準拠）:

- **課題**: マルチリモート選択はインタラクティブ I/O を必要とする。テスト可能性のため抽象化が必要。
- **選択肢**:
  - A) `Console.ReadLine()` を WorktreeService に直接埋め込む — テスト不可、開発者ユーザビリティ低
  - B) `IInteractionService` インターフェース + `ConsoleInteractionService` 実装 — テスト容易、既存サービス抽象化パターンと一貫
- **決定**: B を採用。既存の `IGitService`/`IWorktreeService` パターンの自然な拡張であり、新規外部依存なし。
- **正式 ADR ファイル省略理由**: 既存パターンの踏襲かつ小規模追加のため、このインライン記録をもって Constitution IV の要件を満たすと判断する。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

なし（Constitution違反なし）
