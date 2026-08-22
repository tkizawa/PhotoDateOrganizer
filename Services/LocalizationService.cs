using System;
using System.Globalization;
using PhotoDateOrganizer.Models;

namespace PhotoDateOrganizer.Services;

/// <summary>
/// アプリケーション全体の多言語（日本語・英語）表示を管理するサービスクラス
/// </summary>
public class LocalizationService
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public static Strings Strings => Instance.CurrentStrings;

    public Strings CurrentStrings { get; private set; }

    public bool IsJapanese { get; private set; }

    public LocalizationService()
    {
        // OSの表示言語（UI Culture）が日本語系であれば日本語、それ以外は英語を適用
        var culture = CultureInfo.CurrentUICulture;
        IsJapanese = culture.Name.StartsWith("ja", StringComparison.OrdinalIgnoreCase);
        CurrentStrings = IsJapanese ? new JapaneseStrings() : new EnglishStrings();
    }

    /// <summary>
    /// テストや切り替え用：明示的にカルチャを設定します。
    /// </summary>
    public void SetCulture(CultureInfo culture)
    {
        IsJapanese = culture.Name.StartsWith("ja", StringComparison.OrdinalIgnoreCase);
        CurrentStrings = IsJapanese ? new JapaneseStrings() : new EnglishStrings();
    }
}

/// <summary>
/// 多言語リソースの基底・共通インターフェースクラス
/// </summary>
public abstract class Strings
{
    public abstract string AppTitle { get; }
    public abstract string AppSubtitle { get; }
    public abstract string WindowTitleFormat { get; }

    // ヘッダーボタン
    public abstract string ImportSettingsButton { get; }
    public abstract string ImportSettingsTooltip { get; }
    public abstract string ExportSettingsButton { get; }
    public abstract string ExportSettingsTooltip { get; }
    public abstract string OpenDestinationButton { get; }
    public abstract string OpenDestinationTooltip { get; }

    // フォルダ設定カード
    public abstract string FolderSetupTitle { get; }
    public abstract string SourceFolderLabel { get; }
    public abstract string SourceFolderPlaceholder { get; }
    public abstract string DestinationFolderLabel { get; }
    public abstract string DestinationFolderPlaceholder { get; }
    public abstract string BrowseButton { get; }

    // クラウドファイル設定
    public abstract string CloudFilesGroupLabel { get; }
    public abstract string CloudModeDownloadOption { get; }
    public abstract string CloudModeDownloadTooltip { get; }
    public abstract string CloudModeSkipOption { get; }
    public abstract string CloudModeSkipTooltip { get; }
    public abstract string SupportedFormatsInfo { get; }

    // コントロールボタン・状態
    public abstract string StartOrganizingButton { get; }
    public abstract string CancelButton { get; }
    public abstract string StatusReady { get; }
    public abstract string StatusProcessing { get; }
    public abstract string StatusCancelled { get; }
    public abstract string StatusCompletedFormat { get; }
    public abstract string StatusCompletedWithFallbackFormat { get; }
    public abstract string StatusErrorFormat { get; }

    // 進捗・統計カード
    public abstract string ProgressAndStatsTitle { get; }
    public abstract string ProcessingFileLabel { get; }
    public abstract string FallbackNoticeFormat { get; }
    public abstract string FallbackProgressNoticeFormat { get; }
    public abstract string StatTotal { get; }
    public abstract string StatCopied { get; }
    public abstract string StatFallback { get; }
    public abstract string StatFallbackTooltip { get; }
    public abstract string StatSkipped { get; }
    public abstract string StatError { get; }

    // ログエリア
    public abstract string LogTitle { get; }
    public abstract string ClearLogsButton { get; }
    public abstract string ClearLogsTooltip { get; }

    // ログレベルバッジ
    public abstract string BadgeSuccess { get; }
    public abstract string BadgeWarning { get; }
    public abstract string BadgeError { get; }
    public abstract string BadgeInfo { get; }

    // ログメッセージ & 通知
    public abstract string LogStartOrganizing { get; }
    public abstract string LogSourceSetFormat { get; }
    public abstract string LogDestinationSetFormat { get; }
    public abstract string LogSettingsImportedFormat { get; }
    public abstract string LogSettingsExportedFormat { get; }
    public abstract string LogSettingsImportErrorFormat { get; }
    public abstract string LogSettingsExportErrorFormat { get; }
    public abstract string LogErrorSourceNotExist { get; }
    public abstract string JsonFileFilterName { get; }

    // 整理処理実行中のログ & ステータス
    public abstract string StatusScanning { get; }
    public abstract string LogScanningStartedFormat { get; }
    public abstract string StatusScanCompletedFormat { get; }
    public abstract string LogScanCompletedFormat { get; }
    public abstract string StatusCloudSkipFormat { get; }
    public abstract string LogCloudSkipFormat { get; }
    public abstract string StatusDownloadingFormat { get; }
    public abstract string LogDownloadingFormat { get; }
    public abstract string StatusDownloadErrorFormat { get; }
    public abstract string LogDownloadErrorFormat { get; }
    public abstract string StatusDuplicateSkipFormat { get; }
    public abstract string LogDuplicateSkipFormat { get; }
    public abstract string StatusCopiedFormat { get; }
    public abstract string LogCopiedFormat { get; }
    public abstract string NoteExifFormat { get; }
    public abstract string NoteVideoFormat { get; }
    public abstract string NoteFilenameFallbackFormat { get; }
    public abstract string NoteModifiedFallbackFormat { get; }
    public abstract string NoteCreationFallbackFormat { get; }
    public abstract string LogCloudAccessErrorFormat { get; }
    public abstract string LogGenericErrorFormat { get; }
    public abstract string LogCancelledSummaryFormat { get; }
    public abstract string LogCompletionSummaryFormat { get; }
    public abstract string LogCompletionSummaryWithFallbackFormat { get; }

    // 免責事項ダイアログ
    public abstract string DisclaimerDialogTitle { get; }
    public abstract string DisclaimerAcceptButton { get; }
    public abstract string DisclaimerDeclineButton { get; }
    public abstract string DisclaimerCautionBanner { get; }
    public abstract string DisclaimerItem1Title { get; }
    public abstract string DisclaimerItem1Body { get; }
    public abstract string DisclaimerItem2Title { get; }
    public abstract string DisclaimerItem2Body { get; }
    public abstract string DisclaimerItem3Title { get; }
    public abstract string DisclaimerItem3Body { get; }
    public abstract string DisclaimerItem4Title { get; }
    public abstract string DisclaimerItem4Body { get; }
    public abstract string DisclaimerItem5Title { get; }
    public abstract string DisclaimerItem5Body { get; }
    public abstract string DisclaimerItem6Title { get; }
    public abstract string DisclaimerItem6Body { get; }
}

/// <summary>
/// 日本語リソース実装
/// </summary>
public class JapaneseStrings : Strings
{
    public override string AppTitle => "PhotoDateOrganizer";
    public override string AppSubtitle => "iPhoneやデジカメのExif/動画メタデータを解析し、撮影日ごとの階層フォルダ（YYYY\\YYYY-MM\\YYYY-MM-DD）へ自動整理します。";
    public override string WindowTitleFormat => "PhotoDateOrganizer {0} - 写真・動画撮影日時自動整理";

    public override string ImportSettingsButton => "設定インポート";
    public override string ImportSettingsTooltip => "設定ファイル (JSON) をインポート";
    public override string ExportSettingsButton => "設定エクスポート";
    public override string ExportSettingsTooltip => "現在の設定をJSONファイルへエクスポート";
    public override string OpenDestinationButton => "出力先を開く";
    public override string OpenDestinationTooltip => "出力先フォルダをエクスプローラーで開く";

    public override string FolderSetupTitle => "フォルダ設定";
    public override string SourceFolderLabel => "整理元フォルダ (写真・動画の保存場所):";
    public override string SourceFolderPlaceholder => @"例: C:\Users\Username\Pictures\iPhone";
    public override string DestinationFolderLabel => "整理後出力先フォルダ:";
    public override string DestinationFolderPlaceholder => @"例: D:\OrganizedPhotos";
    public override string BrowseButton => "選択...";

    public override string CloudFilesGroupLabel => "OneDrive / SharePoint オンライン専用（未ダウンロード）ファイルの扱い:";
    public override string CloudModeDownloadOption => "オンライン専用ファイルをダウンロードして整理する（推奨）";
    public override string CloudModeDownloadTooltip => "未ダウンロードのファイルもクラウドから取得してExif解析・整理コピーを行います。";
    public override string CloudModeSkipOption => "オンライン専用ファイルはスキップする（通信なし）";
    public override string CloudModeSkipTooltip => "通信量を発生させず、ローカルにダウンロード済みのファイルのみを整理対象とします。";
    public override string SupportedFormatsInfo => "対象形式: .jpg, .jpeg, .heic, .png, .mov, .mp4（同名ファイルはハッシュ比較で自動判別・連番付与）";

    public override string StartOrganizingButton => "整理を開始する";
    public override string CancelButton => "キャンセル";
    public override string StatusReady => "準備完了: 整理元フォルダと出力先フォルダを選択してください。";
    public override string StatusProcessing => "整理処理中...";
    public override string StatusCancelled => "処理がキャンセルされました。";
    public override string StatusCompletedFormat => "整理完了: {0} 件コピー, {1} 件スキップ, {2} 件エラー ({3})";
    public override string StatusCompletedWithFallbackFormat => "整理完了: {0} 件コピー (うち {1} 件はExif欠損), {2} 件スキップ ({3})";
    public override string StatusErrorFormat => "エラー: {0}";

    public override string ProgressAndStatsTitle => "処理進捗 & 統計";
    public override string ProcessingFileLabel => "処理中ファイル:";
    public override string FallbackNoticeFormat => "💡 注意: {0} 件のファイルはExifメタデータが無いため、ファイル名またはタイムスタンプから判定しました。";
    public override string FallbackProgressNoticeFormat => "💡 注意: {0} 件のファイルはExif欠損のため、ファイル名またはタイムスタンプから判定中です。";
    public override string StatTotal => "総検出数";
    public override string StatCopied => "コピー完了";
    public override string StatFallback => "Exif欠損";
    public override string StatFallbackTooltip => "Exifメタデータが無いためファイル名やタイムスタンプを使用した件数";
    public override string StatSkipped => "スキップ";
    public override string StatError => "エラー";

    public override string LogTitle => "処理ログ";
    public override string ClearLogsButton => "クリア";
    public override string ClearLogsTooltip => "ログをクリア";

    public override string BadgeSuccess => "✓ 成功";
    public override string BadgeWarning => "⚠ 警告";
    public override string BadgeError => "✕ エラー";
    public override string BadgeInfo => "ℹ 情報";

    public override string LogStartOrganizing => "整理処理を開始します...";
    public override string LogSourceSetFormat => "整理元フォルダを設定: {0}";
    public override string LogDestinationSetFormat => "出力先フォルダを設定: {0}";
    public override string LogSettingsImportedFormat => "設定をインポートしました: {0}";
    public override string LogSettingsExportedFormat => "設定をエクスポートしました: {0}";
    public override string LogSettingsImportErrorFormat => "設定インポートエラー: {0}";
    public override string LogSettingsExportErrorFormat => "設定エクスポートエラー: {0}";
    public override string LogErrorSourceNotExist => "エラー: 整理元フォルダが存在しません。";
    public override string JsonFileFilterName => "JSON ファイル (*.json)";

    public override string StatusScanning => "ファイルをスキャン中...";
    public override string LogScanningStartedFormat => "スキャン開始: {0}";
    public override string StatusScanCompletedFormat => "スキャン完了: {0} 件の対象ファイルが見つかりました";
    public override string LogScanCompletedFormat => "{0} 件の対象写真・動画ファイルを検出しました。";
    public override string StatusCloudSkipFormat => "スキップ: {0}（OneDrive/クラウド専用ファイル）";
    public override string LogCloudSkipFormat => "[スキップ] クラウド専用ファイルのためスキップしました（ローカルに未ダウンロード）: {0}";
    public override string StatusDownloadingFormat => "ダウンロード中: {0} (クラウドから取得中...)";
    public override string LogDownloadingFormat => "[ダウンロード中] クラウド専用ファイルを一時ダウンロードしています: {0}";
    public override string StatusDownloadErrorFormat => "エラー: {0} (ダウンロード失敗)";
    public override string LogDownloadErrorFormat => "[エラー] {0}: クラウドからのダウンロードに失敗しました。";
    public override string StatusDuplicateSkipFormat => "スキップ: {0}（同一ファイルが既に存在）";
    public override string LogDuplicateSkipFormat => "[スキップ] 同一ファイルが既に存在します: {0} -> {1}";
    public override string StatusCopiedFormat => "コピー完了: {0}";
    public override string LogCopiedFormat => "[コピー] {0} -> {1}{2}";
    public override string NoteExifFormat => " (Exif: {0:yyyy-MM-dd HH:mm:ss})";
    public override string NoteVideoFormat => " (動画メタデータ: {0:yyyy-MM-dd HH:mm:ss})";
    public override string NoteFilenameFallbackFormat => " (💡 Exif欠損: ファイル名から「{0:yyyy-MM-dd}」を推定)";
    public override string NoteModifiedFallbackFormat => " (⚠ Exif欠損: ファイル更新日時 {0:yyyy-MM-dd} を使用)";
    public override string NoteCreationFallbackFormat => " (⚠ Exif欠損: ファイル作成日時 {0:yyyy-MM-dd} を使用)";
    public override string LogCloudAccessErrorFormat => "[エラー] {0}: OneDrive/クラウド専用ファイルへのアクセスに失敗しました。ローカルにダウンロードされていないか、同期が停止している可能性があります。";
    public override string LogGenericErrorFormat => "[エラー] {0}: {1}";
    public override string LogCancelledSummaryFormat => "処理が中断されました。(コピー済み: {0} 件, スキップ: {1} 件)";
    public override string LogCompletionSummaryFormat => "完了: 合計 {0} 件 (コピー: {1} 件, スキップ: {2} 件, エラー: {3} 件) 所要時間: {4}";
    public override string LogCompletionSummaryWithFallbackFormat => "完了: 合計 {0} 件 (コピー: {1} 件, スキップ: {2} 件, エラー: {3} 件) ※うち {4} 件はExif欠損のためファイル名/作成日時から判定 所要時間: {5}";

    public override string DisclaimerDialogTitle => "⚠️ ご利用上の注意事項・免責事項 (Disclaimer)";
    public override string DisclaimerAcceptButton => "同意して利用を開始する";
    public override string DisclaimerDeclineButton => "同意しない (終了)";
    public override string DisclaimerCautionBanner => "本ソフトウェアをご利用いただく前に、以下の注意事項および免責事項を必ずご確認ください。";
    public override string DisclaimerItem1Title => "1. 原本ファイルの保持とユーザーによる整理・削除の注意";
    public override string DisclaimerItem1Body => "本アプリは写真・動画ファイルを日付フォルダへ「コピー」するツールであり、整理元（原本）のファイルを削除・移動・変更する処理は一切行いません。ただし、本アプリによる整理完了後にユーザー自身が原本ファイルを整理・削除する際の誤削除等には十分にご注意ください。";
    public override string DisclaimerItem2Title => "2. 無保証 (AS-IS)";
    public override string DisclaimerItem2Body => "本ソフトウェアは現状有姿（AS-IS）で提供され、明示的・黙示的を問わず、その正確性、完全性、特定目的への適合性についていかなる保証も行いません。";
    public override string DisclaimerItem3Title => "3. 事前テストの実施";
    public override string DisclaimerItem3Body => "重要な本番データや大容量フォルダに適用する前に、必ず影響のないテスト用フォルダを作成し、ファイルコピーおよび日時分類の動作を十分に検証した上でご利用ください。";
    public override string DisclaimerItem4Title => "4. メタデータおよび撮影日時の判定について";
    public override string DisclaimerItem4Body => "Exif や QuickTime などのメタデータが存在しない場合や破損している場合は、ファイルシステムのタイムスタンプ（作成日時等）が代用されます。SNS保存画像や編集済み動画等では正確な撮影日時とならない場合があります。";
    public override string DisclaimerItem5Title => "5. クラウド専用ファイル（OneDrive等）と通信環境に関する注意";
    public override string DisclaimerItem5Body => "OneDrive や SharePoint などのオンライン専用（未ダウンロード）ファイルを処理対象とする場合、ネットワーク経由での自動ダウンロードが発生します。通信量や従量制課金環境にご注意ください（設定によりスキップすることも可能です）。";
    public override string DisclaimerItem6Title => "6. 免責事項 (開発者の責任について)";
    public override string DisclaimerItem6Body => "本ソフトウェアの使用、設定の誤り、ネットワーク障害、ファイルコピーやメタデータ解析処理等により生じたいかなる損害（データの消失、破損、業務の中断、利益の損失等を含むがこれらに限定されない）について、開発者は一切の責任を負いません。バックアップ等の安全対策は利用者自身の責任で行ってください。";
}

/// <summary>
/// 英語リソース実装 (English Strings)
/// </summary>
public class EnglishStrings : Strings
{
    public override string AppTitle => "PhotoDateOrganizer";
    public override string AppSubtitle => "Automatically organizes iPhone and camera photos/videos into hierarchical date folders (YYYY\\YYYY-MM\\YYYY-MM-DD) by parsing Exif/metadata.";
    public override string WindowTitleFormat => "PhotoDateOrganizer {0} - Date-Based Photo & Video Organizer";

    public override string ImportSettingsButton => "Import Settings";
    public override string ImportSettingsTooltip => "Import configuration from a JSON file";
    public override string ExportSettingsButton => "Export Settings";
    public override string ExportSettingsTooltip => "Export current configuration to a JSON file";
    public override string OpenDestinationButton => "Open Destination";
    public override string OpenDestinationTooltip => "Open the output destination folder in File Explorer";

    public override string FolderSetupTitle => "Folder Setup";
    public override string SourceFolderLabel => "Source Folder (where photos/videos are located):";
    public override string SourceFolderPlaceholder => @"e.g. C:\Users\Username\Pictures\iPhone";
    public override string DestinationFolderLabel => "Destination Folder:";
    public override string DestinationFolderPlaceholder => @"e.g. D:\OrganizedPhotos";
    public override string BrowseButton => "Browse...";

    public override string CloudFilesGroupLabel => "Handling of OneDrive / SharePoint Online-only (unhydrated) files:";
    public override string CloudModeDownloadOption => "Download online-only files and organize (Recommended)";
    public override string CloudModeDownloadTooltip => "Downloads online-only files on demand from cloud to parse Exif and copy them.";
    public override string CloudModeSkipOption => "Skip online-only files (No network usage)";
    public override string CloudModeSkipTooltip => "Organizes only files already stored locally on disk without downloading.";
    public override string SupportedFormatsInfo => "Supported formats: .jpg, .jpeg, .heic, .png, .mov, .mp4 (Duplicates are distinguished via hash comparison)";

    public override string StartOrganizingButton => "Start Organizing";
    public override string CancelButton => "Cancel";
    public override string StatusReady => "Ready: Select a source folder and destination folder.";
    public override string StatusProcessing => "Organizing files...";
    public override string StatusCancelled => "Operation was cancelled.";
    public override string StatusCompletedFormat => "Completed: {0} copied, {1} skipped, {2} error(s) ({3})";
    public override string StatusCompletedWithFallbackFormat => "Completed: {0} copied ({1} missing Exif), {2} skipped ({3})";
    public override string StatusErrorFormat => "Error: {0}";

    public override string ProgressAndStatsTitle => "Progress & Statistics";
    public override string ProcessingFileLabel => "Processing:";
    public override string FallbackNoticeFormat => "💡 Note: {0} file(s) had no Exif metadata; date was inferred from filename or timestamp.";
    public override string FallbackProgressNoticeFormat => "💡 Note: {0} file(s) are missing Exif; inferring date from filename or timestamp.";
    public override string StatTotal => "Total Found";
    public override string StatCopied => "Copied";
    public override string StatFallback => "Missing Exif";
    public override string StatFallbackTooltip => "Number of files organized using filename or file system timestamps due to missing Exif";
    public override string StatSkipped => "Skipped";
    public override string StatError => "Errors";

    public override string LogTitle => "Activity Log";
    public override string ClearLogsButton => "Clear";
    public override string ClearLogsTooltip => "Clear log entries";

    public override string BadgeSuccess => "✓ Success";
    public override string BadgeWarning => "⚠ Warning";
    public override string BadgeError => "✕ Error";
    public override string BadgeInfo => "ℹ Info";

    public override string LogStartOrganizing => "Starting organization process...";
    public override string LogSourceSetFormat => "Source folder set: {0}";
    public override string LogDestinationSetFormat => "Destination folder set: {0}";
    public override string LogSettingsImportedFormat => "Settings imported: {0}";
    public override string LogSettingsExportedFormat => "Settings exported: {0}";
    public override string LogSettingsImportErrorFormat => "Failed to import settings: {0}";
    public override string LogSettingsExportErrorFormat => "Failed to export settings: {0}";
    public override string LogErrorSourceNotExist => "Error: Source folder does not exist.";
    public override string JsonFileFilterName => "JSON Files (*.json)";

    public override string StatusScanning => "Scanning files...";
    public override string LogScanningStartedFormat => "Scan started: {0}";
    public override string StatusScanCompletedFormat => "Scan completed: Found {0} target file(s)";
    public override string LogScanCompletedFormat => "Detected {0} photo/video file(s).";
    public override string StatusCloudSkipFormat => "Skipped: {0} (Cloud-only file)";
    public override string LogCloudSkipFormat => "[Skip] Skipped cloud-only file (not downloaded locally): {0}";
    public override string StatusDownloadingFormat => "Downloading: {0} (fetching from cloud...)";
    public override string LogDownloadingFormat => "[Downloading] Fetching cloud-only file on demand: {0}";
    public override string StatusDownloadErrorFormat => "Error: {0} (Download failed)";
    public override string LogDownloadErrorFormat => "[Error] {0}: Failed to download from cloud.";
    public override string StatusDuplicateSkipFormat => "Skipped: {0} (Identical file already exists)";
    public override string LogDuplicateSkipFormat => "[Skip] Identical file already exists: {0} -> {1}";
    public override string StatusCopiedFormat => "Copied: {0}";
    public override string LogCopiedFormat => "[Copy] {0} -> {1}{2}";
    public override string NoteExifFormat => " (Exif: {0:yyyy-MM-dd HH:mm:ss})";
    public override string NoteVideoFormat => " (Video metadata: {0:yyyy-MM-dd HH:mm:ss})";
    public override string NoteFilenameFallbackFormat => " (💡 Missing Exif: Inferred {0:yyyy-MM-dd} from filename)";
    public override string NoteModifiedFallbackFormat => " (⚠ Missing Exif: Used file modified date {0:yyyy-MM-dd})";
    public override string NoteCreationFallbackFormat => " (⚠ Missing Exif: Used file creation date {0:yyyy-MM-dd})";
    public override string LogCloudAccessErrorFormat => "[Error] {0}: Failed to access cloud-only file. It may not be downloaded locally or sync is paused.";
    public override string LogGenericErrorFormat => "[Error] {0}: {1}";
    public override string LogCancelledSummaryFormat => "Operation cancelled. (Copied: {0}, Skipped: {1})";
    public override string LogCompletionSummaryFormat => "Completed: Total {0} file(s) (Copied: {1}, Skipped: {2}, Errors: {3}) Duration: {4}";
    public override string LogCompletionSummaryWithFallbackFormat => "Completed: Total {0} file(s) (Copied: {1}, Skipped: {2}, Errors: {3}) *{4} missing Exif; inferred from filename/date Duration: {5}";

    public override string DisclaimerDialogTitle => "⚠️ Notice and Disclaimer";
    public override string DisclaimerAcceptButton => "Accept and Start";
    public override string DisclaimerDeclineButton => "Decline (Exit)";
    public override string DisclaimerCautionBanner => "Please review the following terms and disclaimer carefully before using this software.";
    public override string DisclaimerItem1Title => "1. Original File Preservation & User Deletion Caution";
    public override string DisclaimerItem1Body => "This app copies photos and videos into date folders. It NEVER deletes, moves, or alters files in the source folder. Please exercise caution if you choose to manually delete or clean up original files after organization.";
    public override string DisclaimerItem2Title => "2. AS-IS Warranty Disclaimer";
    public override string DisclaimerItem2Body => "This software is provided 'AS-IS', without warranty of any kind, express or implied, including but not limited to the warranties of merchantability or fitness for a particular purpose.";
    public override string DisclaimerItem3Title => "3. Prior Testing Required";
    public override string DisclaimerItem3Body => "Before running on critical production folders or large libraries, always test on a small sample directory to verify copying and organization behavior.";
    public override string DisclaimerItem4Title => "4. Metadata & Date Inferences";
    public override string DisclaimerItem4Body => "If Exif or QuickTime metadata is missing or corrupted, file system creation/modification timestamps will be used. Social media downloads or edited media may not reflect original capture dates.";
    public override string DisclaimerItem5Title => "5. Cloud-Only Files (OneDrive) & Bandwidth Notice";
    public override string DisclaimerItem5Body => "When organizing online-only cloud files, network bandwidth will be consumed to download them. Please check your data plan or use the skip option if on metered connections.";
    public override string DisclaimerItem6Title => "6. Limitation of Liability";
    public override string DisclaimerItem6Body => "The developer shall not be liable for any direct, indirect, or incidental damages (including data loss, corruption, or interruption) arising from the use or inability to use this software.";
}
