namespace PhotoDateOrganizer.Models;

public enum OrganizePhase
{
    Scanning,
    Organizing,
    Completed,
    Cancelled,
    Failed
}

public class OrganizeProgress
{
    public OrganizePhase Phase { get; init; }
    public int ProcessedCount { get; init; }
    public int TotalCount { get; init; }
    public int CopiedCount { get; init; }
    public int SkippedCount { get; init; }
    public int ErrorCount { get; init; }
    public int FallbackCount { get; init; }
    public string? CurrentFilePath { get; init; }
    public string? StatusMessage { get; init; }
    public LogEntry? NewLogEntry { get; init; }

    public double Percentage => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100.0 : 0.0;
}
