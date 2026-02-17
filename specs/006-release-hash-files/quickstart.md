# クイックスタート: ダウンロードの検証

**機能**: リリースバイナリのハッシュファイル
**対象**: wtツールのエンドユーザー
**日付**: 2026-02-14

## 概要

wtツールをダウンロードした後、ファイルが改ざんされていないことを確認するためにSHA256ハッシュを検証できます。このガイドでは、各プラットフォームでの検証方法を説明します。

---

## 検証が必要な理由

ダウンロード中にファイルが破損したり、悪意のある改ざんが行われたりする可能性があります。SHA256ハッシュを検証することで:

- ✅ ファイルが公式リリースと完全に一致することを確認
- ✅ ダウンロード中の破損を検出
- ✅ 悪意のある改ざんから保護

---

## ダウンロード

GitHubのリリースページから、お使いのプラットフォームに対応するバイナリとハッシュファイルをダウンロードします。

**必要なファイル**:
1. バイナリファイル(例: `wt-v1.0.0-windows-x64.exe`)
2. ハッシュファイル(例: `wt-v1.0.0-windows-x64.exe.sha256`)

または:

1. 複数のバイナリファイル
2. `SHA256SUMS` ファイル(すべてのバイナリのハッシュを含む)

---

## Windows での検証

### 前提条件

- PowerShell 5.1以降(Windows 10/11に標準搭載)

### 個別ハッシュファイルで検証

ダウンロードしたバイナリと同じフォルダで PowerShell を開き、以下のコマンドを実行します:

```powershell
# ダウンロードしたファイルのハッシュを計算
$hash = (Get-FileHash .\wt-v1.0.0-windows-x64.exe -Algorithm SHA256).Hash

# ハッシュファイルから期待値を読み込む
$expected = (Get-Content .\wt-v1.0.0-windows-x64.exe.sha256).Split(" ")[0]

# 比較
if ($hash -eq $expected) {
    Write-Host "✓ ハッシュ検証成功: ファイルは正常です" -ForegroundColor Green
} else {
    Write-Host "✗ ハッシュ不一致: ファイルが破損または改ざんされています" -ForegroundColor Red
}
```

### SHA256SUMS ファイルで一括検証

複数のバイナリを検証する場合:

```powershell
Get-Content SHA256SUMS | ForEach-Object {
    $expected, $file = $_ -split '  '
    if (Test-Path $file) {
        $actual = (Get-FileHash $file -Algorithm SHA256).Hash
        if ($actual -eq $expected) {
            Write-Host "✓ $file" -ForegroundColor Green
        } else {
            Write-Host "✗ $file - ハッシュ不一致!" -ForegroundColor Red
        }
    } else {
        Write-Host "- $file - ファイルが見つかりません" -ForegroundColor Yellow
    }
}
```

---

## Linux での検証

### 前提条件

- `sha256sum` コマンド(ほとんどのLinuxディストリビューションに標準搭載)

### 個別ハッシュファイルで検証

ダウンロードしたバイナリと同じディレクトリで以下のコマンドを実行します:

```bash
sha256sum -c wt-v1.0.0-linux-x64.sha256
```

**成功時の出力**:
```
wt-v1.0.0-linux-x64: OK
```

**失敗時の出力**:
```
wt-v1.0.0-linux-x64: FAILED
sha256sum: WARNING: 1 computed checksum did NOT match
```

### SHA256SUMS ファイルで一括検証

複数のバイナリを一度に検証する場合:

```bash
sha256sum -c SHA256SUMS
```

**成功時の出力**:
```
wt-v1.0.0-linux-x64: OK
wt-v1.0.0-linux-arm: OK
```

### 特定のファイルのみ検証

SHA256SUMS から特定のファイルだけを検証する場合:

```bash
grep wt-v1.0.0-linux-x64 SHA256SUMS | sha256sum -c
```

---

## macOS での検証

### 前提条件

- `shasum` コマンド(macOSに標準搭載)

### 個別ハッシュファイルで検証

ダウンロードしたバイナリと同じディレクトリで以下のコマンドを実行します:

```bash
shasum -a 256 -c wt-v1.0.0-macos-arm64.sha256
```

**成功時の出力**:
```
wt-v1.0.0-macos-arm64: OK
```

**失敗時の出力**:
```
wt-v1.0.0-macos-arm64: FAILED
shasum: WARNING: 1 computed checksum did NOT match
```

### SHA256SUMS ファイルで一括検証

複数のバイナリを一度に検証する場合:

```bash
shasum -a 256 -c SHA256SUMS
```

### 代替方法: openssl を使用

```bash
# ハッシュを計算
openssl sha256 wt-v1.0.0-macos-arm64

# 出力例:
# SHA256(wt-v1.0.0-macos-arm64)= a1b2c3d4e5f6...

# ハッシュファイルの内容と手動で比較
cat wt-v1.0.0-macos-arm64.sha256
```

---

## トラブルシューティング

### ハッシュが一致しない場合

1. **ファイルを再ダウンロード**: ダウンロード中に破損した可能性があります
2. **ハッシュファイルも再ダウンロード**: ハッシュファイル自体が破損している可能性があります
3. **公式リリースページから直接ダウンロード**: 信頼できるソースからダウンロードしていることを確認
4. **それでも一致しない場合**: GitHubのIssueで報告してください

### ファイルが見つからないエラー

**原因**: バイナリとハッシュファイルが同じディレクトリにない

**解決方法**:
- 両方のファイルを同じフォルダに配置
- または、ファイルパスを絶対パスで指定

### コマンドが見つからないエラー

**Linux**:
```bash
# Debian/Ubuntu
sudo apt-get install coreutils

# Fedora/RHEL
sudo dnf install coreutils
```

**macOS**:
```bash
# GNU coreutils をインストール(sha256sum を使いたい場合)
brew install coreutils

# sha256sum の代わりに gsha256sum を使用
gsha256sum -c wt-v1.0.0-macos-arm64.sha256
```

---

## よくある質問

### Q: SHA256SUMS と個別の.sha256ファイルの違いは?

**A**:
- **SHA256SUMS**: すべてのバイナリのハッシュが1つのファイルに含まれています。複数のファイルを一度に検証する場合に便利です。
- **個別の.sha256ファイル**: 各バイナリに対応するハッシュファイルです。1つのバイナリだけをダウンロード・検証する場合に便利です。

どちらも同じハッシュ値を含んでおり、お好みの方法を選択できます。

### Q: ハッシュ検証は必須ですか?

**A**: 必須ではありませんが、強く推奨します。特に:
- 企業環境や本番環境で使用する場合
- セキュリティが重要な用途で使用する場合
- 公式以外のミラーからダウンロードした場合

### Q: SHA256SUMS.asc ファイルは何ですか?

**A**: これは SHA256SUMS ファイルのGPG署名です。より高度なセキュリティ検証を行いたい場合に使用します。GPG公開鍵を使用して署名を検証することで、ハッシュファイル自体が改ざんされていないことを確認できます。

GPG検証の詳細については、プロジェクトの SECURITY.md またはドキュメントを参照してください。

### Q: ハッシュ値は大文字・小文字を区別しますか?

**A**: いいえ、SHA256ハッシュの比較では大文字・小文字は区別されません。`A1B2C3...` と `a1b2c3...` は同じハッシュとして扱われます。

---

## 次のステップ

検証が成功したら:

1. **バイナリを適切な場所に移動**:
   - Linux/macOS: `/usr/local/bin/` または `~/.local/bin/`
   - Windows: `C:\Program Files\wt\` または PATHに追加されたディレクトリ

2. **実行権限を付与**(Linux/macOS):
   ```bash
   chmod +x wt-v1.0.0-linux-x64
   ```

3. **wtツールを使用開始**:
   ```bash
   wt --version
   wt --help
   ```

詳細な使い方については、[ユーザーガイド](../../docs/user-guide.md)を参照してください。

---

## 関連リソース

- [GitHubリリースページ](https://github.com/kuju63/wt/releases)
- [SHA256とは?](https://ja.wikipedia.org/wiki/SHA-2)
- [ファイル整合性検証のベストプラクティス](https://help.ubuntu.com/community/HowToSHA256SUM)
- [PowerShell Get-FileHash ドキュメント](https://learn.microsoft.com/ja-jp/powershell/module/microsoft.powershell.utility/get-filehash)

---

**サポートが必要な場合**: [GitHub Issues](https://github.com/kuju63/wt/issues)で質問またはバグ報告を行ってください。
