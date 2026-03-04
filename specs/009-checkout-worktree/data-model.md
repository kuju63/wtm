# Data Model: Checkout Existing Branch as Worktree

**Date**: 2026-03-04
**Status**: Complete

---

## Entities

### 1. `CheckoutWorktreeOptions` (NEW)

**ファイル**: `wt.cli/Models/CheckoutWorktreeOptions.cs`
**用途**: `checkout` コマンドの入力オプションを保持する

| フィールド | 型 | 必須 | 説明 |
|-----------|-----|------|------|
| `BranchName` | `string` | ✅ | チェックアウト対象のブランチ名（ローカルまたはリモート） |
| `Remote` | `string?` | - | `--remote <name>` で指定されたリモート名（null = 自動選択） |
| `Fetch` | `bool` | - | `--fetch` フラグ（true = fetch して最新化） |
| `EditorType` | `string?` | - | `--editor (-e)` で指定されたエディタ種別 |
| `OutputFormat` | `OutputFormat` | - | `--output` 出力形式（Human / Json） |
| `Verbose` | `bool` | - | `--verbose (-v)` 詳細出力 |

**バリデーション**:
- `BranchName`: null/空不可、`Validators.ValidateBranchName()` による命名規則チェック
- `Remote`: null または空白なしの非空文字列

---

### 2. `RemoteBranchInfo` (NEW)

**ファイル**: `wt.cli/Models/RemoteBranchInfo.cs`
**用途**: リモートトラッキング参照の解析結果を保持する

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `RemoteName` | `string` | リモート名（例: `origin`） |
| `BranchName` | `string` | ブランチ名（例: `feature/review-me`） |
| `FullRef` | `string` | フルリファレンス（例: `origin/feature/review-me`） |

---

### 3. `ErrorCodes` (MODIFY: 新コード追加)

**ファイル**: `wt.cli/Models/ErrorCodes.cs`

追加するエラーコード:

| 定数名 | コード値 | 説明 |
|--------|---------|------|
| `RemoteNotFound` | `"RM001"` | 指定リモートが設定されていない |
| `RemoteFetchFailed` | `"RM002"` | リモートへのフェッチ失敗（ネットワーク/認証） |
| `BranchNotFoundAnywhere` | `"RM003"` | ブランチがローカルにもどのリモートにも存在しない |

`GetSolution()` への追加:
- `RM001`: `"git remote -v で設定済みリモートを確認し、--remote フラグに正しいリモート名を指定してください"`
- `RM002`: `"ネットワーク接続と認証情報を確認し、git fetch <remote> を手動実行してください"`
- `RM003`: `"git branch -a でブランチ一覧を確認してください。--fetch フラグを付けてリモートを最新化してから再試行することもできます"`

---

### 4. `IInteractionService` (NEW)

**ファイル**: `wt.cli/Services/Interaction/IInteractionService.cs`
**用途**: インタラクティブなユーザー選択のテスト可能な抽象化

```csharp
public interface IInteractionService
{
    /// <summary>
    /// ユーザーに選択肢を提示し、選択されたインデックスを返す。
    /// キャンセルされた場合は null を返す。
    /// </summary>
    Task<int?> SelectAsync(string prompt, IReadOnlyList<string> choices, CancellationToken cancellationToken = default);
}
```

---

### 5. `ConsoleInteractionService` (NEW)

**ファイル**: `wt.cli/Services/Interaction/ConsoleInteractionService.cs`
**用途**: `IInteractionService` の標準入出力実装

| 入力 | 動作 |
|------|------|
| 有効な番号（1-N） | 対応するインデックス（0始まり）を返す |
| 空エントリ / `q` / `Q` | null を返す（キャンセル） |
| 無効な入力 | エラーメッセージを表示して再プロンプト（最大3回） |
| 3回失敗 | null を返す（キャンセル） |

---

## IGitService 拡張メソッド (MODIFY)

**ファイル**: `wt.cli/Services/Git/IGitService.cs`

新規追加メソッド:

```csharp
/// <summary>git remote で設定済みリモート一覧を取得する。</summary>
Task<CommandResult<IReadOnlyList<string>>> GetRemotesAsync(CancellationToken cancellationToken = default);

/// <summary>
/// git branch -r の出力からリモートトラッキング参照を取得する。
/// branchName が指定された場合はそのブランチ名を持つ参照のみ返す。
/// </summary>
Task<CommandResult<IReadOnlyList<RemoteBranchInfo>>> GetRemoteTrackingBranchesAsync(
    string? branchName = null,
    CancellationToken cancellationToken = default);

/// <summary>git fetch &lt;remote&gt; を実行する。</summary>
Task<CommandResult<Unit>> FetchFromRemoteAsync(string remote, CancellationToken cancellationToken = default);

/// <summary>
/// ローカルブランチの upstream リモートを取得する。
/// upstream 未設定の場合は null を返す（エラーではない）。
/// </summary>
Task<CommandResult<string?>> GetBranchUpstreamRemoteAsync(string branchName, CancellationToken cancellationToken = default);

/// <summary>
/// リモートトラッキングブランチからワークツリーを作成する。
/// git worktree add --track -b &lt;branch&gt; &lt;path&gt; &lt;remote&gt;/&lt;branch&gt;
/// </summary>
Task<CommandResult<Unit>> AddWorktreeFromRemoteAsync(
    string worktreePath,
    string branchName,
    string remoteName,
    CancellationToken cancellationToken = default);
```

---

## IWorktreeService 拡張メソッド (MODIFY)

**ファイル**: `wt.cli/Services/Worktree/IWorktreeService.cs`

新規追加メソッド:

```csharp
/// <summary>
/// 既存ブランチをワークツリーとしてチェックアウトする。
/// ローカルブランチを優先し、存在しない場合はリモートを検索する。
/// </summary>
Task<CommandResult<WorktreeInfo>> CheckoutWorktreeAsync(
    CheckoutWorktreeOptions options,
    IInteractionService interactionService,
    CancellationToken cancellationToken = default);
```

---

## State Transitions: CheckoutWorktreeAsync フロー

```text
[開始]
  │
  ▼
[入力検証]
  ├─ 失敗 → CommandResult.Failure (BR001)
  │
  ▼
[Git リポジトリ確認]
  ├─ 非リポジトリ → CommandResult.Failure (GIT001)
  │
  ▼
[--fetchフラグ確認 + ローカルブランチ検索]
  │
  ├─ ローカルブランチ存在 + --fetch あり
  │     │
  │     ▼
  │   [upstream リモート取得]
  │     ├─ upstream あり → git fetch <remote> → 失敗時は RM002
  │     └─ upstream なし → 警告メッセージ表示（継続）
  │     │
  │     ▼
  │   [ローカルブランチ使用パスへ]
  │
  ├─ ローカルブランチ存在 + --fetch なし
  │     └─ [ローカルブランチ使用パスへ]
  │
  └─ ローカルブランチ不在
        │
        ▼
      [--fetchフラグ確認]
        ├─ --fetch あり → git fetch (全リモートまたは --remote 指定リモート)
        │                  └─ 失敗時は RM002
        │
        ▼
      [リモートトラッキング参照検索 (git branch -r)]
        ├─ 0件 → CommandResult.Failure (RM003)
        ├─ 1件 → 自動選択
        └─ 複数件
              ├─ --remote 指定あり → 該当リモートを選択
              │   └─ 該当なし → CommandResult.Failure (RM001)
              └─ --remote 指定なし → インタラクティブ選択プロンプト
                    └─ キャンセル → CommandResult.Failure (ユーザーキャンセル)

[ブランチ既チェックアウト確認]
  └─ 既存ワークツリーあり → CommandResult.Failure (BR002 + 既存パス情報)

[ワークツリーパス解決 (PathHelper)]
  └─ パス競合 → CommandResult.Failure (WT001)

[ワークツリー作成]
  ├─ ローカルブランチ: git worktree add <path> <branch>
  └─ リモートブランチ: git worktree add --track -b <branch> <path> <remote>/<branch>
     └─ 失敗 → CommandResult.Failure (WT002)

[エディタ起動 (オプション)]
  └─ エディタ未設定/失敗は警告のみ（ワークツリー作成成功を維持）

[完了: CommandResult.Success(WorktreeInfo)]
```
