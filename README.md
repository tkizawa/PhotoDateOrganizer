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

2. **3階層フォルダ自動生成**
   - 撮影日（例: `2026-05-10 14:30:00`）に基づき、以下の階層を作成してコピーします。
   - `[出力先ルート] \ YYYY \ YYYY-MM \ YYYY-MM-DD \ [ファイル名]`
   - *(例: `D:\Photos\2026\2026-05\2026-05-10\IMG_1234.HEIC`)*

3. **安全な重複・衝突回避**
   - 同名ファイルが存在する場合、ファイルサイズと MD5 ハッシュ値を比較。
   - **同一内容:** コピーをスキップし、ログに記録。
   - **異なる内容:** `IMG_1234_1.HEIC`, `IMG_1234_2.HEIC` のように連番を付与して安全にコピー。

4. **モダンな WinUI 3 UI**
   - Windows 11 の Mica 背景素材、Fluent Design、ダーク/ライトテーマ対応。
   - リアルタイムプログレスバー (`ProgressBar`)、総ファイル数/コピー数/スキップ数/エラー数の統計バッジ。
   - 処理中ファイル名表示と、種別ごとに色分けされた処理ログビュー (`ListView`)。
   - `async/await` による完全非同期処理と、いつでも安全に中断できる「キャンセル」機能。

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
├── MainWindow.xaml                 # メイン画面 XAML (Mica, 進捗バー, ログ, 統計)
├── MainWindow.xaml.cs              # FolderPicker HWND 連携, ウィンドウ設定
├── Models/
│   ├── LogEntry.cs                 # 処理ログモデル (Info, Success, Warning, Error)
│   ├── OrganizeProgress.cs         # リアルタイム進捗通知モデル
│   └── OrganizeResult.cs           # 実行結果サマリー
├── Services/
│   ├── IPhotoOrganizerService.cs    # 写真整理サービス インターフェース
│   └── PhotoOrganizerService.cs     # メタデータ解析・重複衝突判定・コピーロジック
├── ViewModels/
│   └── MainViewModel.cs            # MVVM ViewModel (バインディング, コマンド, キャンセル制御)
├── PhotoDateOrganizer.Tests/       # xUnit 単体テストプロジェクト
│   ├── PhotoDateOrganizer.Tests.csproj
│   └── PhotoOrganizerServiceTests.cs
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
