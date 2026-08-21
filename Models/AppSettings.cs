namespace PhotoDateOrganizer.Models;

public class AppSettings
{
    public string SourceDirectory { get; set; } = string.Empty;
    public string DestinationDirectory { get; set; } = string.Empty;
    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;
    public int WindowWidth { get; set; } = 1100;
    public int WindowHeight { get; set; } = 750;

    /// <summary>
    /// ウィンドウが最大化状態だったかどうか
    /// </summary>
    public bool IsMaximized { get; set; } = false;

    /// <summary>
    /// 初回起動時の免責事項（注意事項・無保証等）に同意済みかどうか
    /// </summary>
    public bool IsDisclaimerAccepted { get; set; } = false;

    /// <summary>
    /// OneDriveやSharePointなどのオンライン専用（未ダウンロード）ファイルの処理モード
    /// </summary>
    public CloudFileHandlingMode CloudFileMode { get; set; } = CloudFileHandlingMode.Download;

    /// <summary>
    /// 表示言語設定 ("auto", "ja", "en")
    /// </summary>
    public string Language { get; set; } = "auto";

    /// <summary>
    /// 以前のバージョンとの互換性のためのフラグ（CloudFileModeと連動）
    /// </summary>
    public bool SkipCloudOnlyFiles
    {
        get => CloudFileMode == CloudFileHandlingMode.Skip;
        set
        {
            if (value && CloudFileMode != CloudFileHandlingMode.Skip)
            {
                CloudFileMode = CloudFileHandlingMode.Skip;
            }
            else if (!value && CloudFileMode == CloudFileHandlingMode.Skip)
            {
                CloudFileMode = CloudFileHandlingMode.Download;
            }
        }
    }
}