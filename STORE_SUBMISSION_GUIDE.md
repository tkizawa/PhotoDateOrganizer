# Microsoft Store 新規登録・提出完全ガイド

本ガイドでは、**PhotoDateOrganizer** を Microsoft Partner Center（Microsoft Store）に新規登録・提出するための手順をステップ・バイ・ステップで解説します。

---

## 1. 事前準備（Partner Center でのアプリ名予約）

1. **[Microsoft Partner Center (パートナーセンター)](https://partner.microsoft.com/dashboard/apps/overview)** にサインインします。
2. 左メニュー「**Apps and games（アプリとゲーム）**」>「**Overview（概要）**」を開き、**「New product（新しい製品）」>「MSIX or PWA app（MSIXまたはPWAアプリ）」** をクリックします。
3. **アプリ名（App name）の予約**:
   - `PhotoDateOrganizer` と入力し、利用可能か確認して予約（Reserve name）します。
4. **アプリ ID 情報の取得**:
   - 作成したアプリのダッシュボードで、左メニューの「**Product management（製品の管理）**」>「**App identity（アプリの識別情報）**」を開きます。
   - 以下の3項目をコピーしておきます：
     - **Package/Identity/Name** (例: `12345YourName.PhotoDateOrganizer`)
     - **Package/Identity/Publisher** (例: `CN=12345678-ABCD-EF01-2345-6789ABCDEF01`)
     - **Package/Properties/PublisherDisplayName** (例: `Your Developer Name`)

### パートナーセンターで予約された識別情報
- **Package/Identity/Name**: `57742TomokazuKizawa.PhotoDateOrganizer`
- **Package/Identity/Publisher**: `CN=963B8572-7B10-48CC-9F90-46F0022D6A68`
- **Package/Properties/PublisherDisplayName**: `Tomokazu Kizawa`
- **Store ID**: `9PFRJ0ZNB4T6`
- **Store URL**: `https://apps.microsoft.com/detail/9PFRJ0ZNB4T6`

---

## 2. MSIX パッケージのビルド

マニフェスト（[Package.appxmanifest](file:///c:/Dev/PhotoDateOrganizer/Package.appxmanifest)）に上記情報が設定済みですので、以下のコマンドを実行するだけでパッケージが生成されます：

```powershell
.\build-msix.ps1
```

> **成果物**:
> スクリプト実行後、`.\MSIX` フォルダ配下に以下が生成されます：
> - `PhotoDateOrganizer_1.0.0.0_bundle.msixbundle` ★ **ストア提出に推奨** (x64 + Arm64 統合版)
> - `PhotoDateOrganizer_1.0.0.0_x64.msix`
> - `PhotoDateOrganizer_1.0.0.0_arm64.msix`

---

## 3. Partner Center での提出項目入力

Partner Center のアプリ概要画面から「**Start your submission（提出を開始）**」をクリックし、以下の各項目を入力します。

### ① 価格と提供状況 (Pricing and availability)
- **価格 (Pricing)**: Free（無料）または希望価格
- **市場 (Markets)**: 全地域（All markets）または Japan / US 等
- **公開日時 (Discoverability)**: Make this product available in the Store（ストアで検索・公開可能にする）

### ② プロパティ (Properties)
- **カテゴリ (Category)**: `Photo & video（写真 & ビデオ）` または `Utilities & tools（ユーティリティ & ツール）`
- **プライバシーポリシー URL (Privacy policy URL)**: GitHub 等のプライバシーポリシー（またはリポジトリの README URL）を指定
- **サポート連絡先 (Support contact info)**: お問い合わせ先メールアドレスまたは Web サイト

### ③ 年齢区分 (Age ratings)
- アンケート（IARCレーティング）に回答します。
  - 暴力、性的表現、ギャンブル、位置情報の共有等はいずれも「No（いいえ）」を選択。
  - 通常は **全年齢対象 (3+ / Everyone)** として即時発行されます。

### ④ パッケージ (Packages)
- `.\MSIX\PhotoDateOrganizer_1.0.0.0_bundle.msixbundle` をドラッグ＆ドロップしてアップロードします。
- ※ x64 および Arm64 の両方がパッケージに含まれていることが自動認識されます。

### ⑤ ストア登録情報 (Store listings)
日本語（Japanese）および 英語（English）の登録情報を入力します。

#### 【日本語登録情報の例】
- **製品名**: `PhotoDateOrganizer`
- **概要 (Description)**:
  ```text
  iPhoneやデジタルカメラで撮影した大量の写真・動画を、Exifやメタデータから正確な撮影日時を解析し、日付ごとの階層フォルダ（年\年-月\年-月-日）へ自動分類・整理するWindowsデスクトップアプリです。

  【主な特徴】
  ・Exif / QuickTime / MP4 メタデータの高精度解析
  ・撮影日別（YYYY\YYYY-MM\YYYY-MM-DD）の自動フォルダ振り分け
  ・動画ファイルの専用サブフォルダ自動分類
  ・ハッシュ比較による重複ファイルの安全なスキップ & 衝突回避
  ・OneDrive / SharePoint オンライン専用ファイルのダウンロード・スキップ制御
  ・Windows 11 Mica 対応の美しく洗練された Fluent UI
  ・原本ファイルを一切削除・改変しない安全設計
  ```
- **主な機能 (Features)**:
  - Exif/動画メタデータに基づく撮影日別自動階層整理
  - 重複判定（MD5ハッシュ照合）
  - クラウドファイル（OneDrive等）への柔軟な対応
  - 日英多言語対応
- **検索キーワード (Search terms)**:
  - `写真整理`, `Exif`, `画像整理`, `動画整理`, `Photo Organizer`, `カメラロール`

---

#### 【英語登録情報の例 (English Listing)】
- **Product name**: `PhotoDateOrganizer`
- **Description**:
  ```text
  PhotoDateOrganizer is a modern, high-performance Windows desktop application designed to organize large volumes of photos and videos from smartphones, digital cameras, and SD cards into clean, date-based folder structures.

  By analyzing internal Exif metadata (JPEG, PNG, HEIC) and QuickTime/MP4 video metadata, it accurately extracts the date and time each photo or video was captured—ensuring accurate organization regardless of file creation timestamps.

  【Key Features】
  • Accurate Date Extraction: Prioritizes Exif SubIFD (DateTimeOriginal) and QuickTime headers.
  • Structured Organization: Automatically creates YYYY\YYYY-MM\YYYY-MM-DD hierarchy (with dedicated subfolders for videos).
  • Safe Deduplication & Conflict Avoidance: Compares file size and MD5 hash to safely skip duplicate files and auto-renumber collisions without data loss.
  • Cloud File Handling: Gracefully detects OneDrive/SharePoint online-only placeholder files, allowing you to skip or download on-demand.
  • Modern Fluent UI: Built with WinUI 3 and Windows 11 Mica material with light/dark theme support.
  • Safe Operation: Original files are never modified or deleted (pure copy operation).
  • Bilingual: Seamless English and Japanese interface based on Windows language settings.
  ```
- **Features**:
  - Accurate date extraction via Exif and video metadata
  - Date-based folder organization (YYYY\YYYY-MM\YYYY-MM-DD)
  - Duplicate detection with MD5 checksum verification
  - OneDrive / cloud-only file support (on-demand download or skip)
  - Dark & Light mode with Windows 11 Fluent UI
  - Non-destructive pure copy operation
- **Search terms**:
  - `Photo Organizer`, `Exif`, `Date Organizer`, `Video Organizer`, `Picture Sort`, `Camera Roll`

---

- **スクリーンショット (Screenshots)**:
  - アプリの操作画面スクリーンショット（1366x768 以上推奨、最低1枚）をアップロード。
- **アプリアイコン**:
  - `Assets\StoreLogo.png` (50x50) または `Assets\Square150x150Logo.png` を使用。

---

## 4. 提出と審査 (Submit to the Store)

1. 全項目の入力が完了し、緑色のチェックマークが付いたことを確認します。
2. 画面右上の「**Submit to the Store（ストアに提出）**」ボタンをクリックします。
3. **審査期間**:
   - 通常 24〜72 時間程度で Microsoft による認定審査（Certification）が行われ、承認後に Microsoft Store で全世界に配信されます。

---

## 5. 将来のアップデート（バージョンアップ）手順

1. [PhotoDateOrganizer.csproj](file:///c:/Dev/PhotoDateOrganizer/PhotoDateOrganizer.csproj) のバージョンを上げる（例: `1.0.0.0` → `1.0.1.0`）。
2. `.\build-msix.ps1 -Version "1.0.1.0"` を実行。
3. Partner Center で「Update（更新）」を作成し、新しい `PhotoDateOrganizer_1.0.1.0_bundle.msixbundle` をアップロードして再提出します。
