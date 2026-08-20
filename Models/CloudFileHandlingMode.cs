namespace PhotoDateOrganizer.Models;

/// <summary>
/// OneDrive / SharePoint 等のオンライン専用（未ダウンロード）ファイルの処理モード
/// </summary>
public enum CloudFileHandlingMode
{
    /// <summary>
    /// 一時ダウンロードしてExif解析・整理コピーを行い、完了後にクラウド専用（空き容量を増やす）に戻す（推奨）
    /// </summary>
    HydrateAndDehydrate = 0,

    /// <summary>
    /// クラウド専用ファイルはスキップする
    /// </summary>
    Skip = 1,

    /// <summary>
    /// クラウドからダウンロードしてローカルにも実体を保持する
    /// </summary>
    DownloadAndKeep = 2
}
