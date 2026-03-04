# Quickstart: `checkout` コマンド実装ガイド

**Date**: 2026-03-04

## 実装チェックリスト（依存順）

```
1. [ ] ErrorCodes に RM001/RM002/RM003 を追加
2. [ ] RemoteBranchInfo モデルを追加
3. [ ] CheckoutWorktreeOptions モデルを追加
4. [ ] IInteractionService インターフェースを追加
5. [ ] ConsoleInteractionService 実装を追加
6. [ ] IGitService に拡張メソッドを追加
7. [ ] GitService に拡張メソッドを実装
8. [ ] IWorktreeService に CheckoutWorktreeAsync を追加
9. [ ] WorktreeService に CheckoutWorktreeAsync を実装
10. [ ] CheckoutCommand を追加
11. [ ] Program.cs に CheckoutCommand を登録
```

---

## TDD 開発順序

### Step 1: エラーコード追加（テスト不要）

`wt.cli/Models/ErrorCodes.cs` に以下を追加:

```csharp
public const string RemoteNotFound = "RM001";
public const string RemoteFetchFailed = "RM002";
public const string BranchNotFoundAnywhere = "RM003";
```

`GetSolution()` メソッドにも対応解決策を追加。

---

### Step 2: モデル追加

**テスト**: `wt.tests/Models/RemoteBranchInfoTests.cs`、`CheckoutWorktreeOptionsTests.cs`

- `RemoteBranchInfo` のプロパティ、`FullRef` フォーマット
- `CheckoutWorktreeOptions.Validate()` の境界値テスト

---

### Step 3: IInteractionService + ConsoleInteractionService

**テスト**: `wt.tests/Services/Interaction/ConsoleInteractionServiceTests.cs`

- 有効入力（1-N）→ 対応インデックスを返す
- 空入力 / `q` → null を返す
- 無効入力3回 → null を返す
- CancellationToken でキャンセル

---

### Step 4: GitService 拡張

**テスト**: `wt.tests/Services/Git/GitServiceTests.cs`（既存ファイルに追加）

- `GetRemotesAsync`: `git remote` 出力パース
- `GetRemoteTrackingBranchesAsync`: `git branch -r` 出力パース（複数リモート、ブランチ名フィルタ）
- `FetchFromRemoteAsync`: 成功/失敗シナリオ
- `GetBranchUpstreamRemoteAsync`: upstream 設定あり/なしシナリオ
- `AddWorktreeFromRemoteAsync`: 成功/失敗シナリオ

**`git branch -r` 出力パース例**:

```
  origin/HEAD -> origin/main
  origin/feature/review-me
  upstream/feature/shared
  origin/feature/shared
```

→ `HEAD -> ...` 行はスキップ

---

### Step 5: WorktreeService.CheckoutWorktreeAsync

**テスト**: `wt.tests/Services/Worktree/WorktreeServiceCheckoutTests.cs`

主要テストシナリオ:

```
- ローカルブランチ存在 → 正常作成
- ローカルブランチ存在 + --fetch + upstream設定済み → fetch後作成
- ローカルブランチ存在 + --fetch + upstream未設定 → 警告後作成
- ローカルブランチ不在 + リモート1件 → 自動選択作成
- ローカルブランチ不在 + リモート複数 + --remote指定なし → IInteractionService呼び出し
- ローカルブランチ不在 + リモート複数 + --remote指定 → プロンプトスキップ
- ブランチ既チェックアウト済み → BR002エラー
- ブランチどこにも存在しない → RM003エラー
- ワークツリーパス競合 → WT001エラー
- インタラクティブ選択キャンセル → ユーザーキャンセルエラー
```

---

### Step 6: CheckoutCommand

**テスト**: `wt.tests/Commands/Worktree/CheckoutCommandTests.cs`

主要テストシナリオ:

```
- 正常実行 → 終了コード0、成功メッセージ
- 正常実行 --output json → JSON出力
- エラー発生 → 終了コード1、エラーメッセージ
- GIT001 → 終了コード2
- --help → ヘルプテキスト
- --remote + --fetch 組み合わせ
```

---

## 主要実装ポイント

### `git branch -r` 出力パース

```csharp
// 出力例:
//   origin/HEAD -> origin/main   ← スキップ
//   origin/feature/review-me
//   upstream/feature/shared

private static RemoteBranchInfo? ParseRemoteTrackingLine(string line)
{
    var trimmed = line.Trim();
    if (trimmed.Contains(" -> ")) return null; // HEAD -> skip

    var slashIndex = trimmed.IndexOf('/');
    if (slashIndex < 0) return null;

    var remote = trimmed[..slashIndex];
    var branch = trimmed[(slashIndex + 1)..];
    return new RemoteBranchInfo(remote, branch, trimmed);
}
```

### `GetBranchUpstreamRemoteAsync` 実装

```bash
git config branch.<branchName>.remote
# 成功時: リモート名を返す
# 失敗時(exit code 1): upstream 未設定 → null を返す（エラーではない）
```

### `AddWorktreeFromRemoteAsync` 実装

```bash
git worktree add --track -b <branch> <path> <remote>/<branch>
```

---

## Program.cs 更新

```csharp
// 既存サービスに加えて追加
var interactionService = new ConsoleInteractionService();

// CheckoutCommand 登録
rootCommand.Subcommands.Add(new CheckoutCommand(worktreeService, interactionService));
```

---

## 既存コマンドとの比較

| 項目 | `create` | `checkout` |
|------|---------|-----------|
| ブランチ作成 | 新規作成（`-b` ベース） | 既存ブランチを使用 |
| リモート操作 | なし | リモートトラッキング参照検索、fetch |
| インタラクション | なし | 複数リモート時の選択プロンプト |
| エディタ起動 | サポート | サポート（同一） |
| パス解決 | PathHelper | PathHelper（同一） |
| 出力形式 | human / json | human / json（同一） |
