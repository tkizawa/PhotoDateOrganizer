# PhotoDateOrganizer (写真・動画撮影日時別自動整理アプリ)

Windows デスクトップ向けのモダンな写真・動画自動整理ツールです。  
指定フォルダ（サブフォルダ含む）内の写真・動画をスキャンし、Exif や QuickTime などのメタデータから撮影日時を厳密に取得して、`YYYY\YYYY-MM\YYYY-MM-DD` 形式の階層フォルダへ自動分類・コピーします。

---

## 🌟 主な機能と特徴

1. **メタデータからの厳密な日時取得 (Exif / QuickTime / MP4)**
   - ファイルシステムのタイムスタンプ（作成日時等）ではなく、Exif・メディアメタデータを最優先で解析します。
   - **対応拡張子:** `.jpg`, `.jpeg`, `.heic`, `.png`, `.mov`, `.mp4`
   - **優先順位:**
     1. Exif: `TagDateTimeOriginal` (ExifSubIFD) -> `TagDateTime` (ExifIFD0) -> `TagDateTimeDigitized`
     2. QuickTime / MP4: `QuickTimeMovieHeaderDirectory.TagCreated` -> `QuickTimeTrackHeaderDirectory.TagCreated`
     3. フォールバック: メタデータが存在しない/破損している場合は `File.GetCreationTime()` を利用し、ログに警告を表示。

2. **日付別階層フォルダ自動生成（写真・動画の自動分類）**
   - 撮影日（例: `2026-05-10 14:30:00`）に基づき、以下の階層を作成してコピーします。
   - **写真ファイル:** `[出力先ルート] \ YYYY \ YYYY-MM \ YYYY-MM-DD \ [ファイル名]`
     *(例: `D:\Photos\2026\2026-05\2026-05-10\IMG_1234.HEIC`)*
   - **動画ファイル:** `[出力先ルート] \ YYYY \ YYYY-MM \ YYYY-MM-DD \ 動画 \ [ファイル名]`
     *(例: `D:\Photos\2026\2026-05\2026-05-10\動画\MOV_1234.MP4`)*

3. **安全な重複・衝突回避 & クラウド専用ファイル（OneDrive等）対応**
   - 同名ファイルが存在する場合、ファイルサイズと MD5 ハッシュ値を比較。
   - **同一内容:** コピーをスキップし、ログに記録。
   - **異なる内容:** `IMG_1234_1.HEIC`, `IMG_1234_2.HEIC` のように連番を付与して安全にコピー。
   - **OneDrive / SharePoint 対応:** ローカルに未ダウンロードの「オンライン専用ファイル（プレースホルダー）」を自動検出し、無駄な大量ダウンロードやアクセスエラー（`0x80070780` 等）を防いで安全にスキップ（UIで切り替え可能）。

4. **多言語（日本語・英語）自動切り替え対応**
   - Windows の表示言語モード（UI Culture）を自動検出し、日本語（`ja` / `ja-JP`）または英語（`en` / `en-US`）にUIテキスト、各種ダイアログ、免責事項、通知メッセージをシームレスに切り替えます。

5. **モダンな WinUI 3 UI**
   - Windows 11 の Mica 背景素材、Fluent Design、ダーク/ライトテーマ対応。
   - リアルタイムプログレスバー (`ProgressBar`)、総ファイル数/コピー数/スキップ数/エラー数の統計バッジ。
   - 処理中ファイル名表示と、種別ごとに色分けされた処理ログビュー (`ListView`)。
   - `async/await` による完全非同期処理と、いつでも安全に中断できる「キャンセル」機能。
   - 終了時のウィンドウ位置・サイズ、各種設定の自動保存と復元。

---

## 🛠 技術スタック

- **言語 / ランタイム:** C# 13 / .NET 10
- **UI フレームワーク:** WinUI 3 (Windows App SDK 1.6 / Unpackaged)
- **アーキテクチャ:** MVVM パターン (`CommunityToolkit.Mvvm`)
- **使用ライブラリ:**
  - `MetadataExtractor` (Exif / QuickTime 解析)
  - `Microsoft.WindowsAppSDK` (WinUI 3 コントロール & Mica)
  - `Microsoft.Windows.SDK.BuildTools`

---

## 📂 プロジェクト構成

```
PhotoDateOrganizer/
├── PhotoDateOrganizer.csproj       # プロジェクト定義 (.NET 10 / WinUI 3 / NuGet参照)
├── app.manifest                    # DPI PerMonitorV2 & Windows 10/11 対応マニフェスト
├── App.xaml / App.xaml.cs          # アプリケーションエントリポイント
├── MainWindow.xaml                 # メイン画面 XAML (Mica, 進捗バー, ログ, 統計, 多言語バインディング)
├── MainWindow.xaml.cs              # FolderPicker HWND 連携, ウィンドウ設定, 免責事項ダイアログ
├── Models/
│   ├── AppSettings.cs              # 設定情報モデル (ウィンドウ位置/サイズ/免責事項同意等)
│   ├── CloudFileHandlingMode.cs    # クラウドファイル処理モード列挙型
│   ├── LogEntry.cs                 # 処理ログモデル (Info, Success, Warning, Error)
│   ├── OrganizeProgress.cs         # リアルタイム進捗通知モデル
│   └── OrganizeResult.cs           # 実行結果サマリー
├── Services/
│   ├── LocalizationService.cs      # 多言語（日本語・英語）リソース管理サービス
│   ├── CloudFileService.cs         # OneDrive/SharePoint クラウドファイル属性判定
│   ├── IPhotoOrganizerService.cs    # 写真整理サービス インターフェース
│   ├── PhotoOrganizerService.cs     # メタデータ解析・重複衝突判定・コピーロジック
│   └── SettingsService.cs          # 設定の保存・読み込み・インポート/エクスポート
├── ViewModels/
│   └── MainViewModel.cs            # MVVM ViewModel (バインディング, コマンド, キャンセル制御)
├── PhotoDateOrganizer.Tests/       # xUnit 単体テストプロジェクト
│   ├── PhotoDateOrganizer.Tests.csproj
│   ├── LocalizationServiceTests.cs # 多言語切り替え単体テスト
│   ├── CloudFileServiceTests.cs    # クラウドファイル判定単体テスト
│   ├── PhotoOrganizerServiceTests.cs # 写真整理・Exif解析単体テスト
│   └── SettingsServiceTests.cs     # 設定保存・復元単体テスト
└── README.md
```

---

## 🚀 セットアップ・ビルド・実行手順

### 1. 前提環境
- **OS:** Windows 10 (バージョン 1809 以降) または Windows 11
- **.NET SDK:** .NET 10 SDK (または .NET 8 / 9)

### 2. ビルド
```powershell
# プロジェクトフォルダへ移動
cd c:\Dev\PhotoDateOrganizer

# ビルドの実行 (ARM64 または x64)
dotnet build
```

### 3. テストの実行
```powershell
# 単体テストの実行
dotnet test PhotoDateOrganizer.Tests\PhotoDateOrganizer.Tests.csproj
```

### 4. アプリケーションの起動
```powershell
# 開発時直接実行
dotnet run
```

### 5. 配布用自己完結型パッケージのパブリッシュ
```powershell
# x64 版自己完結バイナリの出力
dotnet publish -c Release -r win-x64 --self-contained

# ARM64 版自己完結バイナリの出力
dotnet publish -c Release -r win-arm64 --self-contained
```
パブリッシュされた実行ファイルは `bin\Release\net10.0-windows10.0.19041.0\[RID]\publish\` 内に出力されます。

---

## ⚠️ ご利用上の注意事項・免責事項 (Disclaimer)

> [!CAUTION]
> 本ソフトウェアをご利用いただく前に、以下の注意事項および免責事項を必ずご確認ください。

1. **原本ファイルの保持とユーザーによる整理・削除の注意**:
   - 本アプリは写真・動画ファイルを日付フォルダへ「コピー」するツールであり、整理元（原本）のファイルを削除・移動・変更する処理は一切行いません。ただし、本アプリによる整理完了後にユーザー自身が原本ファイルを整理・削除する際の誤削除等には十分にご注意ください。
2. **無保証 (AS-IS)**:
   - 本ソフトウェアは現状有姿（AS-IS）で提供され、明示的・黙示的を問わず、その正確性、完全性、特定目的への適合性についていかなる保証も行いません。
3. **事前テストの実施**:
   - 重要な本番データや大容量フォルダに適用する前に、**必ず影響のないテスト用フォルダを作成し、ファイルコピーおよび日時分類の動作を十分に検証した上でご利用ください**。
4. **メタデータおよび撮影日時の判定について**:
   - Exif や QuickTime などのメタデータが存在しない場合や破損している場合は、ファイルシステムのタイムスタンプ（作成日時等）が代用されます。SNS保存画像や編集済み動画等では正確な撮影日時とならない場合があります。
5. **クラウド専用ファイル（OneDrive等）と通信環境に関する注意**:
   - OneDrive や SharePoint などのオンライン専用（未ダウンロード）ファイルを処理対象とする場合、ネットワーク経由での自動ダウンロードが発生します。通信量や従量制課金環境にご注意ください（設定によりスキップすることも可能です）。
6. **免責事項 (開発者の責任について)**:
   - 本ソフトウェアの使用、設定の誤り、ネットワーク障害、ファイルコピーやメタデータ解析処理等により生じたいかなる損害（データの消失、破損、業務の中断、利益の損失等を含むがこれらに限定されない）について、**開発者は一切の責任を負いません**。バックアップ等の安全対策は利用者自身の責任で行ってください。


