# CLI Contract: `wt checkout` Command

**Date**: 2026-03-04
**Status**: Complete

---

## Command Definition

```
wt checkout <branch-name> [options]
```

### Arguments

| 名前 | 型 | 必須 | 説明 |
|------|-----|------|------|
| `branch-name` | string | ✅ | チェックアウトするブランチ名（ローカルまたはリモート） |

### Options

| フラグ | 短縮 | 型 | デフォルト | 説明 |
|--------|------|----|-----------|------|
| `--remote <name>` | - | string | null | 使用するリモート名を明示指定（インタラクティブプロンプトをスキップ） |
| `--fetch` | - | bool | false | ワークツリー作成前にリモートを fetch して最新化する |
| `--editor <type>` | `-e` | string | null | ワークツリー作成後に起動するエディタ（vscode, vim, emacs, nano, idea） |
| `--output <format>` | `-o` | string | human | 出力形式（human \| json） |
| `--verbose` | `-v` | bool | false | 詳細な進捗メッセージを表示する |

---

## Behavior Contract

### Priority Order (FR-002, FR-003)

1. **ローカルブランチ優先**: 指定ブランチ名がローカルに存在する場合、リモートを参照せずローカルブランチを使用
2. **リモートフォールバック**: ローカルに存在しない場合、キャッシュ済みリモートトラッキング参照を検索（`git branch -r`）

### Remote Selection Logic (FR-004, FR-005, FR-005a)

| 条件 | 動作 |
|------|------|
| リモートが1件のみ | 自動選択（プロンプトなし） |
| リモートが複数件 + `--remote` 指定なし | インタラクティブ選択プロンプトを表示 |
| リモートが複数件 + `--remote <name>` 指定あり | 指定リモートを直接使用（プロンプトスキップ） |
| `--remote <name>` 指定 + そのリモートにブランチなし | エラー (RM001) |

### `--fetch` Flag Behavior (FR-004a)

| 条件 | 動作 |
|------|------|
| ローカルブランチ存在 + upstream 設定済み | `git fetch <remote>` で最新化後にワークツリー作成 |
| ローカルブランチ存在 + upstream 未設定 | 警告を表示し、現在のローカル状態でワークツリー作成を継続 |
| ローカルブランチ不在 + `--remote` 指定あり | 指定リモートのみ fetch |
| ローカルブランチ不在 + `--remote` 指定なし | 全リモートを fetch |

---

## Output Formats

### Success (Human)

```
ワークツリーを作成しました
  ブランチ : feature/review-me
  パス     : /path/to/repo/../feature-review-me
  リモート : origin（リモートブランチの場合のみ表示）
```

### Success (JSON)

```json
{
  "success": true,
  "worktree": {
    "path": "/path/to/repo/../feature-review-me",
    "branch": "feature/review-me",
    "remote": "origin",
    "isDetached": false,
    "createdAt": "2026-03-04T12:00:00Z"
  }
}
```

### Warning (upstream 未設定時)

```
警告: ブランチ 'feature/review-me' に upstream リモートが設定されていません。
      最新化をスキップしてワークツリーを作成します。
      upstream を設定するには: git branch --set-upstream-to=<remote>/<branch> feature/review-me
```

### Error (Human)

```
エラー [RM003]: ブランチ 'feature/nonexistent' がローカルにも
              いずれのリモートにも見つかりませんでした。
解決方法: git branch -a でブランチ一覧を確認してください。
          --fetch フラグを付けてリモートを最新化してから再試行することもできます。
```

### Error (JSON)

```json
{
  "success": false,
  "errorCode": "RM003",
  "message": "ブランチ 'feature/nonexistent' がローカルにもいずれのリモートにも見つかりませんでした。",
  "solution": "git branch -a でブランチ一覧を確認してください。--fetch フラグを付けてリモートを最新化してから再試行することもできます。"
}
```

---

## Error Codes

| コード | HTTP相当 | 終了コード | 発生条件 |
|--------|---------|-----------|---------|
| `BR001` | 400 | 1 | ブランチ名が無効 |
| `BR002` | 409 | 1 | ブランチが既に別のワークツリーでチェックアウト済み |
| `GIT001` | 412 | 2 | Git リポジトリではない |
| `RM001` | 404 | 1 | `--remote` で指定したリモートが存在しない、またはそのリモートにブランチがない |
| `RM002` | 503 | 1 | `git fetch` 失敗（ネットワーク/認証） |
| `RM003` | 404 | 1 | ブランチがローカルにもどのリモートにも存在しない |
| `WT001` | 409 | 1 | ワークツリー作成先パスが既に存在する |
| `WT002` | 500 | 1 | `git worktree add` コマンド失敗 |
| `FS001` | 500 | 1 | ディレクトリ作成失敗 |

---

## Acceptance Scenarios (from spec.md)

### US1: ローカルブランチのチェックアウト

```bash
# Given: feature/review-me がローカルに存在
wt checkout feature/review-me
# → ワークツリー作成成功、パス表示
# → エディタ設定があれば起動

# Given: ブランチが既に別ワークツリーでチェックアウト済み
wt checkout feature/review-me
# → エラー BR002、既存ワークツリーパスを表示
```

### US2: リモートブランチのチェックアウト（単一リモート）

```bash
# Given: feature/remote-only がローカルになく origin にのみ存在
wt checkout feature/remote-only
# → origin から自動選択、ワークツリー作成成功

# Given: リモートにもブランチが存在しない
wt checkout feature/nonexistent
# → エラー RM003

# --fetch フラグで最新化してからチェックアウト
wt checkout --fetch feature/remote-only
```

### US3: 複数リモートでのインタラクティブ選択

```bash
# Given: origin と upstream 両方に feature/shared が存在
wt checkout feature/shared
# → リモート選択プロンプト表示

# --remote で直接指定
wt checkout --remote origin feature/shared
# → プロンプトスキップ、origin から作成

# --remote で存在しないリモートを指定
wt checkout --remote typo feature/shared
# → エラー RM001
```
