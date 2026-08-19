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
    /// OneDriveやSharePointなどのオンライン専用（未ダウンロード）ファイルをスキップするかどうか
    /// </summary>
    public bool SkipCloudOnlyFiles { get; set; } = true;
}
