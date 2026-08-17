using System;

namespace PhotoDateOrganizer.Models;

public class OrganizeResult
{
    public int TotalScanned { get; init; }
    public int CopiedCount { get; init; }
    public int SkippedCount { get; init; }
    public int ErrorCount { get; init; }
    public int FallbackCount { get; init; }
    public TimeSpan Duration { get; init; }
    public bool IsCancelled { get; init; }
    public string? ErrorMessage { get; init; }
}
