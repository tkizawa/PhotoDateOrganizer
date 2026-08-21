namespace PhotoDateOrganizer.Models;

/// <summary>
/// OneDrive / SharePoint 等のオンライン専用（未ダウンロード）ファイルの処理モード
/// </summary>
public enum CloudFileHandlingMode
{
    /// <summary>
    /// クラウド専用ファイルをダウンロードして整理する（推奨）
    /// </summary>
    Download = 0,

    /// <summary>
    /// クラウド専用ファイルはスキップする
    /// </summary>
    Skip = 1,

    /// <summary>
    /// 以前のバージョンとの互換性のための別名（Downloadと同等）
    /// </summary>
    DownloadAndKeep = 0,

    /// <summary>
    /// 以前のバージョンとの互換性のための別名（Downloadと同等）
    /// </summary>
    HydrateAndDehydrate = 0
}