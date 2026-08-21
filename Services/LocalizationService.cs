using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PhotoDateOrganizer.Services;

public enum AppLanguage
{
    Auto,
    Japanese,
    English
}

/// <summary>
/// アプリケーション内で使用される全テキストの定義インターフェース/ベースクラス
/// </summary>
public abstract class AppStrings
{
    // Common
    public abstract string AppTitle { get; }
    public abstract string AppDescription { get; }

    // Header Actions
    public abstract string ImportSettings { get; }
    public abstract string ImportSettingsToolTip { get; }
    public abstract string ExportSettings { get; }
    public abstract string ExportSettingsToolTip { get; }
    public abstract string OpenDestination { get; }
    public abstract string OpenDestinationToolTip { get; }

    // Folder Setup Card
    public abstract string FolderSettingsTitle { get; }
    public abstract string SourceFolderLabel { get; }
    public abstract string SourceFolderPlaceholder { get; }
    public abstract string DestinationFolderLabel { get; }
    public abstract string DestinationFolderPlaceholder { get; }
    public abstract string BrowseButton { get; }

    // Cloud Options
    public abstract string CloudOptionsLabel { get; }
    public abstract string CloudModeDownloadContent { get; }
    public abstract string CloudModeDownloadToolTip { get; }
    public abstract string CloudModeSkipContent { get; }
    public abstract string CloudModeSkipToolTip { get; }
    public abstract string SupportedFormatsInfo { get; }

    // Controls Card
    public abstract string StartButton { get; }
    public abstract string CancelButton { get; }

    // Progress & Statistics Card
    public abstract string ProgressTitle { get; }
    public abstract string CurrentProcessingFileLabel { get; }
    public abstract string StatTotal { get; }
    public abstract string StatCopied { get; }
    public abstract string StatFallback { get; }
    public abstract string StatFallbackToolTip { get; }
    public abstract string StatSkipped { get; }
    public abstract string StatErrors { get; }

    // Activity Log
    public abstract string ActivityLogTitle { get; }
    public abstract string ClearLogsButton { get; }
    public abstract string ClearLogsToolTip { get; }

    // Log Badges
    public abstract string LogLevelSuccess { get; }
    public abstract string LogLevelWarning { get; }
    public abstract string LogLevelError { get; }
    public abstract string LogLevelInfo { get; }

    // Status & Log Messages in ViewModel / Service
    public abstract string ReadyStatus { get; }
    public abstract string SourceFolderNotFound { get; }
    public abstract string SourceFolderSetFormat(string folder);
    public abstract string DestinationFolderSetFormat(string folder);
    public abstract string StartOrganizingProcess { get; }
    public abstract string OperationCancelled { get; }
    public abstract string RequestingCancellation { get; }
    public abstract string CancellationRequestedByUser { get; }
    public abstract string ErrorFormat(string message);
    public abstract string ExceptionErrorFormat(string message);
    public abstract string FallbackNoticeFormat(int count);
    public abstract string FallbackNoticeProgressFormat(int count);
    public abstract string OrganizeCompleteWithFallbackFormat(int copied, int fallback, int skipped, TimeSpan duration);
    public abstract string OrganizeCompleteStandardFormat(int copied, int skipped, int errors, TimeSpan duration);
    public abstract string CannotOpenFolderFormat(string message);
    public abstract string SettingsImportedFormat(string path);
    public abstract string SettingsImportedSummaryFormat(string fileName);
    public abstract string SettingsImportFailedFormat(string message);
    public abstract string SettingsExportedFormat(string path);
    public abstract string SettingsExportedSummaryFormat(string fileName);
    public abstract string SettingsExportFailedFormat(string message);

    // Service Organizing Progress / Logs
    public abstract string ScanningFilesStatus { get; }
    public abstract string ScanningStartLog(string directory);
    public abstract string ScanCompletedStatus(int count);
    public abstract string ScanDetectedLog(int count);
    public abstract string SkipCloudOnlyStatus(string fileName);
    public abstract string SkipCloudOnlyLog(string fileName);
    public abstract string DownloadingCloudFileStatus(string fileName);
    public abstract string DownloadingCloudFileLog(string fileName);
    public abstract string DownloadCloudFileFailedStatus(string fileName);
    public abstract string DownloadCloudFileFailedLog(string fileName);
    public abstract string SkipDuplicateStatus(string fileName);
    public abstract string SkipDuplicateLog(string fileName, string relativePath);
    public abstract string CopyCompleteStatus(string fileName);
    public abstract string CopyLog(string fileName, string relativePath, string note);
    public abstract string ErrorCloudAccessLog(string fileName);
    public abstract string ErrorGeneralLog(string fileName, string message);

    // Note formatting
    public abstract string NoteExif(DateTime date);
    public abstract string NoteQuickTime(DateTime date);
    public abstract string NoteFilenamePattern(DateTime date);
    public abstract string NoteFileModified(DateTime date);
    public abstract string NoteFileCreated(DateTime date);

    // Disclaimer Dialog
    public abstract string DisclaimerDialogTitle { get; }
    public abstract string DisclaimerAgreeButton { get; }
    public abstract string DisclaimerDisagreeButton { get; }
    public abstract string DisclaimerCautionBanner { get; }
    public abstract string DisclaimerItem1Title { get; }
    public abstract string DisclaimerItem1Desc { get; }
    public abstract string DisclaimerItem2Title { get; }
    public abstract string DisclaimerItem2Desc { get; }
    public abstract string DisclaimerItem3Title { get; }
    public abstract string DisclaimerItem3Desc { get; }
    public abstract string DisclaimerItem4Title { get; }
    public abstract string DisclaimerItem4Desc { get; }
    public abstract string DisclaimerItem5Title { get; }
    public abstract string DisclaimerItem5Desc { get; }
    public abstract string DisclaimerItem6Title { get; }
    public abstract string DisclaimerItem6Desc { get; }

    // Pickers
    public abstract string JsonFilePickerFilterName { get; }
}

/// <summary>
/// 日本語リソース実装
/// </summary>
public class JapaneseStrings : AppStrings
{
    public override string AppTitle => "PhotoDateOrganizer - 写真・動画撮影日時自動整理";
    public override string AppDescription => "iPhoneやデジカメのExif/動画メタデータを解析し、撮影日ごとの階層フォルダ（YYYY\\YYYY-MM\\YYYY-MM-DD）へ自動整理します。";

    public override string ImportSettings => "設定インポート";
    public override string ImportSettingsToolTip => "設定ファイル (JSON) をインポート";
    public override string ExportSettings => "設定エクスポート";
    public override string ExportSettingsToolTip => "現在の設定をJSONファイルへエクスポート";
    public override string OpenDestination => "出力先を開く";
    public override string OpenDestinationToolTip => "出力先フォルダをエクスプローラーで開く";

    public override string FolderSettingsTitle => "フォルダ設定";
    public override string SourceFolderLabel => "整理元フォルダ (写真・動画の保存場所):";
    public override string SourceFolderPlaceholder => @"例: C:\Users\Username\Pictures\iPhone";
    public override string DestinationFolderLabel => "整理後出力先フォルダ:";
    public override string DestinationFolderPlaceholder => @"例: D:\OrganizedPhotos";
    public override string BrowseButton => "選択...";

    public override string CloudOptionsLabel => "OneDrive / SharePoint オンライン専用（未ダウンロード）ファイルの扱い:";
    public override string CloudModeDownloadContent => "オンライン専用ファイルをダウンロードして整理する（推奨）";
    public override string CloudModeDownloadToolTip => "未ダウンロードのファイルもクラウドから取得してExif解析・整理コピーを行います。";
    public override string CloudModeSkipContent => "オンライン専用ファイルはスキップする（通信なし）";
    public override string CloudModeSkipToolTip => "通信量を発生させず、ローカルにダウンロード済みのファイルのみを整理対象とします。";
    public override string SupportedFormatsInfo => "対象形式: .jpg, .jpeg, .heic, .png, .mov, .mp4（同名ファイルはハッシュ比較で自動判別・連番付与）";

    public override string StartButton => "整理を開始する";
    public override string CancelButton => "キャンセル";

    public override string ProgressTitle => "処理進捗 & 統計";
    public override string CurrentProcessingFileLabel => "処理中ファイル:";
    public override string StatTotal => "総検出数";
    public override string StatCopied => "コピー完了";
    public override string StatFallback => "Exif欠損";
    public override string StatFallbackToolTip => "Exifメタデータが無いためファイル名やタイムスタンプを使用した件数";
    public override string StatSkipped => "スキップ";
    public override string StatErrors => "エラー";

    public override string ActivityLogTitle => "処理ログ";
    public override string ClearLogsButton => "クリア";
    public override string ClearLogsToolTip => "ログをクリア";

    public override string LogLevelSuccess => "✓ 成功";
    public override string LogLevelWarning => "⚠ 警告";
    public override string LogLevelError => "✕ エラー";
    public override string LogLevelInfo => "ℹ 情報";

    public override string ReadyStatus => "準備完了: ソースフォルダと出力先フォルダを選択してください。";
    public override string SourceFolderNotFound => "エラー: ソースフォルダが存在しません。";
    public override string SourceFolderSetFormat(string folder) => $"ソースフォルダを設定: {folder}";
    public override string DestinationFolderSetFormat(string folder) => $"出力先フォルダを設定: {folder}";
    public override string StartOrganizingProcess => "整理処理を開始します...";
    public override string OperationCancelled => "処理がキャンセルされました。";
    public override string RequestingCancellation => "キャンセルを要求中...";
    public override string CancellationRequestedByUser => "ユーザーによるキャンセルが要求されました。";
    public override string ErrorFormat(string message) => $"エラー: {message}";
    public override string ExceptionErrorFormat(string message) => $"例外エラー: {message}";
    public override string FallbackNoticeFormat(int count) => $"💡 注意: {count} 件のファイルはExifメタデータが無いため、ファイル名またはタイムスタンプから判定しました。";
    public override string FallbackNoticeProgressFormat(int count) => $"💡 注意: {count} 件のファイルはExif欠損のため、ファイル名またはタイムスタンプから判定中です。";
    public override string OrganizeCompleteWithFallbackFormat(int copied, int fallback, int skipped, TimeSpan duration) =>
        $"整理完了: {copied} 件コピー (うち {fallback} 件はExif欠損), {skipped} 件スキップ ({duration:mm\\:ss})";
    public override string OrganizeCompleteStandardFormat(int copied, int skipped, int errors, TimeSpan duration) =>
        $"整理完了: {copied} 件コピー, {skipped} 件スキップ, {errors} 件エラー ({duration:mm\\:ss})";
    public override string CannotOpenFolderFormat(string message) => $"フォルダを開けませんでした: {message}";
    public override string SettingsImportedFormat(string path) => $"設定をインポートしました: {path}";
    public override string SettingsImportedSummaryFormat(string fileName) => $"設定をインポートしました ({fileName})";
    public override string SettingsImportFailedFormat(string message) => $"設定のインポートに失敗しました: {message}";
    public override string SettingsExportedFormat(string path) => $"設定をエクスポートしました: {path}";
    public override string SettingsExportedSummaryFormat(string fileName) => $"設定をエクスポートしました ({fileName})";
    public override string SettingsExportFailedFormat(string message) => $"設定のエクスポートに失敗しました: {message}";

    public override string ScanningFilesStatus => "ファイルをスキャン中...";
    public override string ScanningStartLog(string directory) => $"スキャン開始: {directory}";
    public override string ScanCompletedStatus(int count) => $"スキャン完了: {count} 件の対象ファイルが見つかりました";
    public override string ScanDetectedLog(int count) => $"{count} 件の対象写真・動画ファイルを検出しました。";
    public override string SkipCloudOnlyStatus(string fileName) => $"スキップ: {fileName}（OneDrive/クラウド専用ファイル）";
    public override string SkipCloudOnlyLog(string fileName) => $"[スキップ] クラウド専用ファイルのためスキップしました（ローカルに未ダウンロード）: {fileName}";
    public override string DownloadingCloudFileStatus(string fileName) => $"ダウンロード中: {fileName} (クラウドから取得中...)";
    public override string DownloadingCloudFileLog(string fileName) => $"[ダウンロード中] クラウド専用ファイルを一時ダウンロードしています: {fileName}";
    public override string DownloadCloudFileFailedStatus(string fileName) => $"エラー: {fileName} (ダウンロード失敗)";
    public override string DownloadCloudFileFailedLog(string fileName) => $"[エラー] {fileName}: クラウドからのダウンロードに失敗しました。";
    public override string SkipDuplicateStatus(string fileName) => $"スキップ: {fileName}（同一ファイルが既に存在）";
    public override string SkipDuplicateLog(string fileName, string relativePath) => $"[スキップ] 同一ファイルが既に存在します: {fileName} -> {relativePath}";
    public override string CopyCompleteStatus(string fileName) => $"コピー完了: {fileName}";
    public override string CopyLog(string fileName, string relativePath, string note) => $"[コピー] {fileName} -> {relativePath}{note}";
    public override string ErrorCloudAccessLog(string fileName) => $"[エラー] {fileName}: OneDrive/クラウド専用ファイルへのアクセスに失敗しました。ローカルにダウンロードされていないか、同期が停止している可能性があります。";
    public override string ErrorGeneralLog(string fileName, string message) => $"[エラー] {fileName}: {message}";

    public override string NoteExif(DateTime date) => $" (Exif: {date:yyyy-MM-dd HH:mm:ss})";
    public override string NoteQuickTime(DateTime date) => $" (動画メタデータ: {date:yyyy-MM-dd HH:mm:ss})";
    public override string NoteFilenamePattern(DateTime date) => $" (💡 Exif欠損: ファイル名から「{date:yyyy-MM-dd}」を推定)";
    public override string NoteFileModified(DateTime date) => $" (⚠ Exif欠損: ファイル更新日時 {date:yyyy-MM-dd} を使用)";
    public override string NoteFileCreated(DateTime date) => $" (⚠ Exif欠損: ファイル作成日時 {date:yyyy-MM-dd} を使用)";

    public override string DisclaimerDialogTitle => "⚠️ ご利用上の注意事項・免責事項 (Disclaimer)";
    public override string DisclaimerAgreeButton => "同意して利用を開始する";
    public override string DisclaimerDisagreeButton => "同意しない (終了)";
    public override string DisclaimerCautionBanner => "本ソフトウェアをご利用いただく前に、以下の注意事項および免責事項を必ずご確認ください。";
    public override string DisclaimerItem1Title => "1. 原本ファイルの保持とユーザーによる整理・削除の注意";
    public override string DisclaimerItem1Desc => "本アプリは写真・動画ファイルを日付フォルダへ「コピー」するツールであり、整理元（原本）のファイルを削除・移動・変更する処理は一切行いません。ただし、本アプリによる整理完了後にユーザー自身が原本ファイルを整理・削除する際の誤削除等には十分にご注意ください。";
    public override string DisclaimerItem2Title => "2. 無保証 (AS-IS)";
    public override string DisclaimerItem2Desc => "本ソフトウェアは現状有姿（AS-IS）で提供され、明示的・黙示的を問わず、その正確性、完全性、特定目的への適合性についていかなる保証も行いません。";
    public override string DisclaimerItem3Title => "3. 事前テストの実施";
    public override string DisclaimerItem3Desc => "重要な本番データや大容量フォルダに適用する前に、必ず影響のないテスト用フォルダを作成し、ファイルコピーおよび日時分類の動作を十分に検証した上でご利用ください。";
    public override string DisclaimerItem4Title => "4. メタデータおよび撮影日時の判定について";
    public override string DisclaimerItem4Desc => "Exif や QuickTime などのメタデータが存在しない場合や破損している場合は、ファイルシステムのタイムスタンプ（作成日時等）が代用されます。SNS保存画像や編集済み動画等では正確な撮影日時とならない場合があります。";
    public override string DisclaimerItem5Title => "5. クラウド専用ファイル（OneDrive等）と通信環境に関する注意";
    public override string DisclaimerItem5Desc => "OneDrive や SharePoint などのオンライン専用（未ダウンロード）ファイルを処理対象とする場合、ネットワーク経由での自動ダウンロードが発生します。通信量や従量制課金環境にご注意ください（設定によりスキップすることも可能です）。";
    public override string DisclaimerItem6Title => "6. 免責事項 (開発者の責任について)";
    public override string DisclaimerItem6Desc => "本ソフトウェアの使用、設定の誤り、ネットワーク障害、ファイルコピーやメタデータ解析処理等により生じたいかなる損害（データの消失、破損、業務の中断、利益の損失等を含むがこれらに限定されない）について、開発者は一切の責任を負いません。バックアップ等の安全対策は利用者自身の責任で行ってください。";

    public override string JsonFilePickerFilterName => "JSON ファイル (*.json)";
}

/// <summary>
/// 英語リソース実装
/// </summary>
public class EnglishStrings : AppStrings
{
    public override string AppTitle => "PhotoDateOrganizer - Auto Organize Photos & Videos by Date";
    public override string AppDescription => "Analyzes Exif/video metadata from iPhone and digital cameras, and automatically organizes files into date-based hierarchical folders (YYYY\\YYYY-MM\\YYYY-MM-DD).";

    public override string ImportSettings => "Import Settings";
    public override string ImportSettingsToolTip => "Import settings file (JSON)";
    public override string ExportSettings => "Export Settings";
    public override string ExportSettingsToolTip => "Export current settings to a JSON file";
    public override string OpenDestination => "Open Destination";
    public override string OpenDestinationToolTip => "Open destination folder in File Explorer";

    public override string FolderSettingsTitle => "Folder Settings";
    public override string SourceFolderLabel => "Source Folder (Photos & Videos Location):";
    public override string SourceFolderPlaceholder => @"e.g. C:\Users\Username\Pictures\iPhone";
    public override string DestinationFolderLabel => "Destination Folder:";
    public override string DestinationFolderPlaceholder => @"e.g. D:\OrganizedPhotos";
    public override string BrowseButton => "Browse...";

    public override string CloudOptionsLabel => "Handling of OneDrive / SharePoint Online-only (not downloaded) files:";
    public override string CloudModeDownloadContent => "Download and organize online-only files (Recommended)";
    public override string CloudModeDownloadToolTip => "Downloads online-only files from the cloud to read Exif metadata and copy.";
    public override string CloudModeSkipContent => "Skip online-only files (No network usage)";
    public override string CloudModeSkipToolTip => "Avoids network traffic and only organizes files that are already downloaded locally.";
    public override string SupportedFormatsInfo => "Supported formats: .jpg, .jpeg, .heic, .png, .mov, .mp4 (Duplicates handled via hash comparison & sequential naming)";

    public override string StartButton => "Start Organizing";
    public override string CancelButton => "Cancel";

    public override string ProgressTitle => "Progress & Statistics";
    public override string CurrentProcessingFileLabel => "Current File:";
    public override string StatTotal => "Total";
    public override string StatCopied => "Copied";
    public override string StatFallback => "No Exif";
    public override string StatFallbackToolTip => "Files organized using filename or timestamp due to missing Exif metadata";
    public override string StatSkipped => "Skipped";
    public override string StatErrors => "Errors";

    public override string ActivityLogTitle => "Activity Log";
    public override string ClearLogsButton => "Clear";
    public override string ClearLogsToolTip => "Clear activity log";

    public override string LogLevelSuccess => "✓ Success";
    public override string LogLevelWarning => "⚠ Warning";
    public override string LogLevelError => "✕ Error";
    public override string LogLevelInfo => "ℹ Info";

    public override string ReadyStatus => "Ready: Please select a source and destination folder.";
    public override string SourceFolderNotFound => "Error: Source folder does not exist.";
    public override string SourceFolderSetFormat(string folder) => $"Source folder set: {folder}";
    public override string DestinationFolderSetFormat(string folder) => $"Destination folder set: {folder}";
    public override string StartOrganizingProcess => "Starting organization process...";
    public override string OperationCancelled => "Operation cancelled.";
    public override string RequestingCancellation => "Requesting cancellation...";
    public override string CancellationRequestedByUser => "Cancellation requested by user.";
    public override string ErrorFormat(string message) => $"Error: {message}";
    public override string ExceptionErrorFormat(string message) => $"Exception: {message}";
    public override string FallbackNoticeFormat(int count) => $"💡 Notice: {count} file(s) had no Exif metadata and were determined from filenames or timestamps.";
    public override string FallbackNoticeProgressFormat(int count) => $"💡 Notice: {count} file(s) missing Exif; determining from filenames or timestamps.";
    public override string OrganizeCompleteWithFallbackFormat(int copied, int fallback, int skipped, TimeSpan duration) =>
        $"Complete: {copied} copied ({fallback} without Exif), {skipped} skipped ({duration:mm\\:ss})";
    public override string OrganizeCompleteStandardFormat(int copied, int skipped, int errors, TimeSpan duration) =>
        $"Complete: {copied} copied, {skipped} skipped, {errors} errors ({duration:mm\\:ss})";
    public override string CannotOpenFolderFormat(string message) => $"Could not open folder: {message}";
    public override string SettingsImportedFormat(string path) => $"Settings imported: {path}";
    public override string SettingsImportedSummaryFormat(string fileName) => $"Settings imported ({fileName})";
    public override string SettingsImportFailedFormat(string message) => $"Failed to import settings: {message}";
    public override string SettingsExportedFormat(string path) => $"Settings exported: {path}";
    public override string SettingsExportedSummaryFormat(string fileName) => $"Settings exported ({fileName})";
    public override string SettingsExportFailedFormat(string message) => $"Failed to export settings: {message}";

    public override string ScanningFilesStatus => "Scanning files...";
    public override string ScanningStartLog(string directory) => $"Scan started: {directory}";
    public override string ScanCompletedStatus(int count) => $"Scan completed: {count} file(s) found";
    public override string ScanDetectedLog(int count) => $"Detected {count} target photo/video file(s).";
    public override string SkipCloudOnlyStatus(string fileName) => $"Skipped: {fileName} (OneDrive/Cloud-only file)";
    public override string SkipCloudOnlyLog(string fileName) => $"[Skipped] Cloud-only file skipped (not downloaded locally): {fileName}";
    public override string DownloadingCloudFileStatus(string fileName) => $"Downloading: {fileName} (Fetching from cloud...)";
    public override string DownloadingCloudFileLog(string fileName) => $"[Downloading] Downloading cloud-only file: {fileName}";
    public override string DownloadCloudFileFailedStatus(string fileName) => $"Error: {fileName} (Download failed)";
    public override string DownloadCloudFileFailedLog(string fileName) => $"[Error] {fileName}: Failed to download from cloud.";
    public override string SkipDuplicateStatus(string fileName) => $"Skipped: {fileName} (Identical file already exists)";
    public override string SkipDuplicateLog(string fileName, string relativePath) => $"[Skipped] Identical file already exists: {fileName} -> {relativePath}";
    public override string CopyCompleteStatus(string fileName) => $"Copy complete: {fileName}";
    public override string CopyLog(string fileName, string relativePath, string note) => $"[Copied] {fileName} -> {relativePath}{note}";
    public override string ErrorCloudAccessLog(string fileName) => $"[Error] {fileName}: Failed to access OneDrive/cloud-only file. It may not be downloaded or synchronization may be paused.";
    public override string ErrorGeneralLog(string fileName, string message) => $"[Error] {fileName}: {message}";

    public override string NoteExif(DateTime date) => $" (Exif: {date:yyyy-MM-dd HH:mm:ss})";
    public override string NoteQuickTime(DateTime date) => $" (Video Metadata: {date:yyyy-MM-dd HH:mm:ss})";
    public override string NoteFilenamePattern(DateTime date) => $" (💡 No Exif: estimated \"{date:yyyy-MM-dd}\" from filename)";
    public override string NoteFileModified(DateTime date) => $" (⚠ No Exif: used modified date {date:yyyy-MM-dd})";
    public override string NoteFileCreated(DateTime date) => $" (⚠ No Exif: used created date {date:yyyy-MM-dd})";

    public override string DisclaimerDialogTitle => "⚠️ Terms of Use & Disclaimer";
    public override string DisclaimerAgreeButton => "Agree and Continue";
    public override string DisclaimerDisagreeButton => "Disagree (Exit)";
    public override string DisclaimerCautionBanner => "Please carefully review the following terms of use and disclaimer before using this software.";
    public override string DisclaimerItem1Title => "1. Source File Retention & User Responsibility";
    public override string DisclaimerItem1Desc => "This application copies photos and videos into date-based folders and will never delete, move, or modify source (original) files. However, please be cautious to avoid accidental file deletion when you manually clean up or organize original files after processing.";
    public override string DisclaimerItem2Title => "2. No Warranty (AS-IS)";
    public override string DisclaimerItem2Desc => "This software is provided \"AS-IS\" without warranty of any kind, express or implied, including but not limited to accuracy, completeness, or fitness for a particular purpose.";
    public override string DisclaimerItem3Title => "3. Preliminary Testing";
    public override string DisclaimerItem3Desc => "Before running this tool on important production data or large folders, always test on a safe sample directory to verify copying and date classification behavior.";
    public override string DisclaimerItem4Title => "4. Metadata & Date Identification";
    public override string DisclaimerItem4Desc => "When Exif or QuickTime metadata is absent or damaged, file system timestamps (created date, etc.) are used as fallback. Downloaded social media images or edited videos may not reflect the original shooting date.";
    public override string DisclaimerItem5Title => "5. Cloud-Only Files (OneDrive, etc.) & Network Usage";
    public override string DisclaimerItem5Desc => "Processing online-only (not locally downloaded) files from OneDrive or SharePoint will automatically trigger downloads over the network. Be mindful of data consumption on metered connections (can be skipped in settings).";
    public override string DisclaimerItem6Title => "6. Limitation of Liability";
    public override string DisclaimerItem6Desc => "The developer assumes no liability for any direct, indirect, incidental, or consequential damages (including data loss, corruption, business interruption, or profit loss) resulting from the use of this software. Users are solely responsible for maintaining their own backups.";

    public override string JsonFilePickerFilterName => "JSON Files (*.json)";
}

/// <summary>
/// 多言語管理シングルトンサービス
/// WindowsのUIカルチャー（CultureInfo.CurrentUICulture）から自動判定し、適切な言語リソースを提供します。
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
    public static LocalizationService Current => _instance.Value;

    private AppLanguage _currentLanguage = AppLanguage.Auto;
    private AppStrings _strings;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationService()
    {
        _strings = ResolveStrings(AppLanguage.Auto);
    }

    public AppLanguage Language
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                _strings = ResolveStrings(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Strings));
                OnPropertyChanged(nameof(IsJapanese));
                OnPropertyChanged(nameof(IsEnglish));
            }
        }
    }

    public AppStrings Strings => _strings;

    public bool IsJapanese => _strings is JapaneseStrings;
    public bool IsEnglish => _strings is EnglishStrings;

    /// <summary>
    /// OSのカルチャーまたは指定言語設定から文字列リソースを解決します。
    /// </summary>
    public static AppStrings ResolveStrings(AppLanguage language, CultureInfo? culture = null)
    {
        if (language == AppLanguage.Japanese)
        {
            return new JapaneseStrings();
        }

        if (language == AppLanguage.English)
        {
            return new EnglishStrings();
        }

        // Auto: OSのUIカルチャーを判定
        var uiCulture = culture ?? CultureInfo.CurrentUICulture;
        if (uiCulture.Name.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ||
            uiCulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase))
        {
            return new JapaneseStrings();
        }

        // 日本語以外は英語をデフォルト適用
        return new EnglishStrings();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
