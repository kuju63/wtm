<!--
Sync Impact Report (Version Update):
- Version Change: Initial → 1.0.0
- New Principles Added:
  * I. Developer Usability (CLI優先)
  * II. Cross-Platform (クロスプラットフォーム対応)
  * III. Clean & Secure Code (クリーンでセキュアなコード)
  * IV. Documentation Clarity (ドキュメントの明瞭性)
  * V. Minimal Dependencies (最小限の依存関係)
  * VI. Comprehensive Testing (テストの充実と自動化)
- Templates Requiring Updates:
  ✅ plan-template.md - Constitution Check section validated
  ✅ spec-template.md - Aligned with documentation requirements
  ✅ tasks-template.md - Aligned with testing discipline
- Follow-up TODOs: None
-->

# wtm Project Constitution

## Core Principles

### I. Developer Usability (開発者のユーザビリティ)

**MUST**: CLIインターフェースを最優先とし、操作内容が明瞭で分かりやすいこと

**Rationale**: 多くの開発者はCLIを駆使した効率的な作業を行います。全ての機能はCLIから直感的にアクセス可能であり、コマンド名・引数・出力形式は自己説明的でなければなりません。

**Requirements**:

- コマンドは動詞+名詞の明確な命名規則に従う
- ヘルプメッセージは完全かつ例を含む
- 出力は人間が読める形式とJSON形式の両方をサポート
- エラーメッセージは具体的で実行可能な解決策を含む

### II. Cross-Platform (クロスプラットフォーム)

**MUST**: 主要なOS（Windows、macOS、Linux）で動作すること

**Rationale**: 開発者の環境は多様です。特定のOSに依存しないことで、より多くの開発者がツールを利用できます。

**Requirements**:

- OS固有の機能に依存しない設計
- パス操作はOSに依存しない抽象化を使用
- プラットフォーム固有の処理は明示的に分離
- 各プラットフォームでのCI/CDテストを実施

### III. Clean & Secure Code (クリーンでセキュアなコード)

**MUST**: コードは明瞭で保守性が高く、セキュリティベストプラクティスに従うこと

**Rationale**: AIコードエージェントツールは開発者の信頼を必要とします。セキュリティの脆弱性は重大な影響をもたらします。

**Requirements**:

- 静的コード解析ツールによる継続的な品質チェック
- セキュリティスキャンの自動化（依存関係の脆弱性チェック含む）
- コードレビューでのセキュリティ観点の必須確認
- 機密情報（API キー、トークン等）のハードコード禁止

追加の要件（セキュリティ/例外処理/入力検証等）:

- **例外の扱い**: 空の `catch { }` を禁止する。例外を意図的に無視する場合は、対象となる例外種別と無視理由をコメントで明記し、可能な限りログに記録すること。
- **特定例外の捕捉**: 汎用的な `Exception` の丸抱え捕捉を避け、捕捉する例外は明示的な型に限定すること。必要に応じて再スローまたはラップして呼び出し側へ伝播すること。
- **入力検証**: 外部からの入力は常に検証・正規化を行い、境界値や型の検査を実施すること。
- **秘密情報の管理**: API キーやシークレットはシークレットマネージャ等で管理し、ソースコードやリポジトリにハードコードしないこと。
- **最小権限の原則**: 実行コンテキストやアクセス権は最小権限で構成すること。
- **依存関係の脆弱性対応**: 重大/高リスクの脆弱性は発見から72時間以内に対応計画を提示し、可能であれば速やかに修正すること。
- **重大セキュリティ問題の優先対応**: セキュリティカテゴリが High/Critical の問題はマージ前に解決するか、審査で明確な緩和策を承認すること。

### IV. Documentation Clarity (ドキュメントの明瞭性)

**MUST**: ドキュメントは日本語を優先とし、技術的決定事項はADRとして記録すること

**Rationale**: プロジェクトの意思決定プロセスの透明性と、将来のメンテナンス性を確保します。

**Requirements**:

- ユーザー向けドキュメントは日本語で記述（技術用語は英語を併記）
- 全ての技術的決定はArchitecture Decision Record (ADR)として記録
- ADRには以下を含める：課題、背景、選択肢（Pros/Cons）、決定事項
- コードコメントは英語または日本語（コンテキストに応じて）
- README、ガイド、チュートリアルは日本語優先

### V. Minimal Dependencies (最小限の依存関係)

**MUST**: 依存関係は必要最小限に抑え、サプライチェーンリスクを低減すること

**Rationale**: サプライチェーンアタックの影響を受けにくくし、長期的なメンテナンス性を向上させます。

**Requirements**:

- 新しい依存関係の追加には明示的な正当化が必要
- 依存関係のライセンス互換性を確認
- 定期的な依存関係の見直しと削減
- 可能な限り標準ライブラリを優先使用
- 依存関係の脆弱性スキャンを自動化

### VI. Comprehensive Testing (テストの充実と自動化)

**MUST**: TDD（テスト駆動開発）を採用し、自動化されたテストカバレッジを維持すること

**Rationale**: 高品質なソフトウェアを維持し、リグレッションを防ぎます。

**Requirements**:

- TDDサイクル（Red-Green-Refactor）の厳格な遵守
- ユニットテスト、統合テスト、E2Eテストの適切な組み合わせ
- 新機能には必ずテストが先行
- CI/CDパイプラインでの自動テスト実行
- クリティカルパスのテストカバレッジ維持

### VII. Quantitative Thresholds (閾値)

プロジェクト品質を定量的に担保するため、以下の閾値（デフォルト提案）を定めます。プロジェクトやコンテキストに応じてレビューで調整してください。

- **Method LOC (メソッド行数)**: 50行以内（目標） — 50行を超える場合は分割を検討すること。
- **Cyclomatic Complexity (循環的複雑度)**: 8以下（目標） — 8を超える場合は責務分割と単純化を行うこと。
- **Test Coverage (テスト網羅率)**: プロジェクト全体80%以上、クリティカルパス90%以上（目標）。新機能はユニットテストを先行させること。
- **Dependency Vulnerabilities (依存関係の脆弱性対応)**: 重大（CVSS等級に基づく）な脆弱性は発見から72時間以内に対応計画を提示すること。
- **PR Quality Gate (プルリク品質ゲート)**: 全てのPRは自動テストと主要静的解析をパスしてからマージすること。

これらの閾値はガイドラインであり、特殊ケースではレビューでの合意により例外を許容します。例外が許可される場合はADRに理由を記録してください。

## Development Workflow

### Specification-Driven Development (SDD)

実装前に必ず仕様書を作成し、レビューを経てから実装に進みます。

- Spec Kitを使用した仕様管理
- 仕様書の明確な承認プロセス
- 実装と仕様の継続的な同期

### Test-Driven Development (TDD)

テストを先に書き、それをパスする実装を行います。

- Red: テストを書き、失敗を確認
- Green: テストをパスする最小限の実装
- Refactor: コード品質を向上

### Version Control

- コミットメッセージはConventional Commitsに準拠（英語）
- 基本フォーマット: `<type>: <subject>`
- 主な type: feat, fix, docs, style, refactor, test, chore
- scopeは原則使用しない（必要な場合のみ `<type>(scope): <subject>`）

## Governance

### Amendment Process

憲章の改定は以下のプロセスに従います：

1. 改定提案の文書化（背景、影響範囲、移行計画）
2. チーム内レビューと承認
3. 依存ドキュメント・テンプレートの更新
4. バージョン番号の更新（Semantic Versioning）

### Compliance Review

- 全てのPR/レビューで憲章準拠を確認
- 憲章に反する変更には明示的な正当化が必要
- 複雑さの導入には十分な理由が必要

### Runtime Guidance

日常の開発ガイダンスは `AGENTS.md` ファイルを参照してください。

**Version**: 1.0.0 | **Ratified**: 2026-01-03 | **Last Amended**: 2026-01-03
