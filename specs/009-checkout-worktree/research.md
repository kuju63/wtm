# Research: Checkout Existing Branch as Worktree

**Date**: 2026-03-04
**Status**: Complete — all NEEDS CLARIFICATION resolved

## 1. Git Commands for Remote Branch Discovery

### Decision
キャッシュ済みリモートトラッキング参照の検索には `git branch -r` の出力パースを使用する。リモート一覧は `git remote` で取得。

### Rationale
- `git branch -r` はネットワークアクセス不要でローカルキャッシュ済みリモートトラッキング参照を返す（仕様の「キャッシュ済みリモートトラッキング参照を使用」要件に合致）
- `git ls-remote` はリアルタイムネットワークアクセスが必要なため採用しない（`--fetch` フラグ時のみ使用）
- 出力フォーマット: `  origin/feature/review-me` → `remote = "origin"`, `branch = "feature/review-me"` に分解可能

### Alternatives Considered
| 代替案 | 却下理由 |
|--------|---------|
| `git ls-remote` | ネットワークアクセスが発生し、デフォルト動作の「キャッシュ使用」に反する |
| `git for-each-ref refs/remotes/` | 動作は同等だが `git branch -r` より冗長 |

---

## 2. Creating Worktree Tracking Remote Branch

### Decision
リモートブランチからワークツリーを作成する場合: `git worktree add --track -b <branch> <path> <remote>/<branch>`

ローカルブランチからワークツリーを作成する場合: `git worktree add <path> <branch>`（既存の `AddWorktreeAsync` と同等）

### Rationale
- `--track` オプションにより、作成するローカルブランチが `<remote>/<branch>` を上流として追跡する
- `-b <branch>` でローカルブランチ名を明示的に指定することで、ブランチ名の混乱を防ぐ
- `git worktree add <path> <branch>` はローカルブランチが既に存在する場合に使用（フラグなし）

### Alternatives Considered
| 代替案 | 却下理由 |
|--------|---------|
| `git checkout -b <branch> <remote>/<branch>` + `git worktree add` | 2ステップになり複雑 |
| `git worktree add <path> <remote>/<branch>` (detached HEAD) | detached HEADになり開発不便 |

---

## 3. Interactive Remote Selection (No New Dependencies)

### Decision
インタラクティブプロンプトには `IInteractionService` インターフェースを導入し、`ConsoleInteractionService` がデフォルト実装として `Console.Write` / `Console.ReadLine` を使用する。テストでは Moq でモック可能。

### Rationale
- 新規外部ライブラリ（Spectre.Console等）なしで実装可能（Constitution Principle V 遵守）
- インターフェース抽象化によりテスト容易性を維持（Constitution Principle VI 遵守）
- 実装シンプル: 番号付きリスト表示 → 番号入力 → バリデーション

### Implementation Pattern
```
利用可能なリモートリポジトリ:
  1. origin
  2. upstream
リモートを選択してください (1-2): _
```

- 無効入力は再プロンプト（最大3回でキャンセル）
- Ctrl+C / 空エントリでキャンセル

### Alternatives Considered
| 代替案 | 却下理由 |
|--------|---------|
| Spectre.Console | 新規依存関係（Constitution V 違反） |
| System.CommandLine のインタラクティブ機能 | v2.0.x では対話的選択UIなし |
| `--remote` フラグ強制（プロンプトなし） | ユーザビリティ低下（FR-005 要件に反する） |

---

## 4. Error Code Strategy

### Decision
既存エラーコード体系を拡張する。`RM` プレフィックスで新規カテゴリ「Remote」を追加:

| コード | 意味 |
|--------|------|
| `RM001` | 指定リモートが存在しない |
| `RM002` | リモートへのフェッチ失敗（ネットワークエラー、認証失敗） |
| `RM003` | ブランチがローカルにもどのリモートにも存在しない |

既存コード `WT001`（ワークツリーパス競合）と `BR002`（ブランチ既にチェックアウト中）は再利用。

### Rationale
- 既存の GIT/BR/WT/FS/ED 体系と一貫したパターン
- Remote 操作固有のエラーは専用カテゴリで明示
- `ErrorCodes.GetSolution()` メソッドに対応解決策を追加

---

## 5. Worktree Path Resolution for Remote Branches

### Decision
リモートブランチからのチェックアウト時のパス解決は `PathHelper` の既存ロジックを流用。ブランチ名 `feature/review-me` → ディレクトリ名変換は `create` コマンドと同一規則。

### Rationale
- 仕様の「`PathHelper` と同一の変換規則」（FR-008, Assumptions 参照）に準拠
- 新規ロジック不要

---

## 6. `--fetch` Flag Implementation

### Decision

`--fetch` フラグの目的は**最新のブランチ状態**でワークツリーを作成することにある。そのため、ローカルブランチが存在する場合でも fetch を試みて最新化する。

処理フロー（`--fetch` あり）:
1. ローカルブランチ検索 → 見つかった場合:
   - `git config branch.<branch>.remote` で upstream リモートを取得
   - upstream が設定されている場合: `git fetch <remote> <branch>` で最新化してワークツリー作成
   - upstream が未設定の場合: **警告メッセージを表示**して現在のローカルブランチ状態でワークツリー作成を継続
2. ローカルに見つからない場合:
   - `git remote` でリモート一覧取得
   - 各リモートに `git fetch <remote>` 実行（並列不可、順次実行）
   - `git branch -r` でリモートトラッキング参照を再検索してワークツリー作成

処理フロー（`--fetch` なし、デフォルト）:
1. ローカルブランチ検索 → 見つかれば使用
2. 見つからない場合: `git branch -r` でキャッシュ済みリモートトラッキング参照を検索

### Rationale
- `--fetch` の主目的はブランチの最新化であるため、ローカルブランチでも最新化を試みる
- upstream 未設定ブランチは最新化不可能だが、作業を中断するよりも警告後継続する方がユーザビリティが高い
- フラグなし時はネットワークアクセスなしでパフォーマンス良好

---

## Summary: Resolved Unknowns

| # | Question | Answer |
|---|---------|--------|
| 1 | リモートブランチ検索コマンド | `git branch -r` でキャッシュ参照、`--fetch` 時のみ `git fetch` 実行 |
| 2 | リモートブランチからワークツリー作成 | `git worktree add --track -b <branch> <path> <remote>/<branch>` |
| 3 | インタラクティブプロンプト実装 | `IInteractionService` 抽象化 + `ConsoleInteractionService`（新規依存なし） |
| 4 | 新規エラーコード | `RM001`, `RM002`, `RM003` を `ErrorCodes` に追加 |
| 5 | パス解決 | 既存 `PathHelper` をそのまま流用 |
| 6 | `--fetch` + ローカルブランチあり | upstream 設定済みなら fetch 後最新化、未設定なら警告して現状のまま継続 |
| 7 | `--fetch` + ローカルブランチなし | 全リモートを fetch 後にリモートトラッキング参照を再検索 |
