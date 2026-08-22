using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using MetadataExtractor.Formats.Xmp;
using PhotoDateOrganizer.Models;

namespace PhotoDateOrganizer.Services;

public class PhotoOrganizerService : IPhotoOrganizerService
{
    private readonly ICloudFileService _cloudFileService;

    public PhotoOrganizerService(ICloudFileService? cloudFileService = null)
    {
        _cloudFileService = cloudFileService ?? new CloudFileService();
    }

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".heic",
        ".png",
        ".mov",
        ".mp4"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mov",
        ".mp4"
    };

    public static bool IsVideoFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var ext = Path.GetExtension(filePath);
        return VideoExtensions.Contains(ext);
    }

    private static readonly string[] ExifDateTimeFormats = new[]
    {
        "yyyy:MM:dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy/MM/dd HH:mm:ss",
        "yyyy:MM:dd HH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy:MM:dd",
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "yyyyMMdd_HHmmss",
        "yyyyMMdd"
    };

    // Regex to match date patterns in filenames (e.g. 20230308, 2023-03-08, 2023_03_08, 2023.03.08)
    private static readonly Regex FilenameDateRegex = new(
        @"(?:^|[\D])(?<year>19\d\d|20\d\d)[-_.消]?(?<month>0[1-9]|1[0-2])[-_.消]?(?<day>0[1-9]|[12]\d|3[01])(?:[-_T\s](?<hour>[01]\d|2[0-3])[-_.]?(?<min>[0-5]\d)(?:[-_.]?(?<sec>[0-5]\d))?)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Regex for Unix timestamp in milliseconds (13 digits) or seconds (10 digits)
    private static readonly Regex UnixTimestampRegex = new(
        @"(?:^|[\D])(?<ts>(?:1[5-9]|20)\d{8}(?:\d{3})?)(?:[\D]|$)",
        RegexOptions.Compiled);

    // Windows クラウドファイル属性（OneDrive / SharePoint プレースホルダー）
    private const FileAttributes AttributeRecallOnOpen = (FileAttributes)0x00040000;
    private const FileAttributes AttributeRecallOnDataAccess = (FileAttributes)0x00400000;

    /// <summary>
    /// 指定されたファイルが OneDrive や SharePoint などのオンライン専用（ローカルに未ダウンロード）ファイルかどうかを判定します。
    /// </summary>
    public static bool IsCloudOnlyFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var attributes = File.GetAttributes(filePath);

            // Offline または RecallOnDataAccess または RecallOnOpen 属性がある場合はローカルに実体がない
            return (attributes & FileAttributes.Offline) != 0 ||
                   (attributes & AttributeRecallOnOpen) != 0 ||
                   (attributes & AttributeRecallOnDataAccess) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 例外がクラウドファイル（OneDrive / SharePoint等）のダウンロード失敗や未同期に起因するものか判定します。
    /// </summary>
    public static bool IsCloudFileException(Exception ex)
    {
        // 0x80070780: ERROR_CANT_ACCESS_FILE (ファイルにアクセスできません)
        // 0x800701AA: ERROR_CLOUD_FILE_UNSUCCESSFUL (クラウド操作が失敗しました)
        // 0x80070178 - 0x80070188: クラウドファイル関連エラーコード群
        int hResult = ex.HResult;
        if (hResult == unchecked((int)0x80070780) ||
            hResult == unchecked((int)0x800701AA) ||
            (hResult >= unchecked((int)0x80070178) && hResult <= unchecked((int)0x80070188)))
        {
            return true;
        }

        var message = ex.Message;
        if (message.Contains("0x80070780", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("0x800701AA", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("クラウド", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("cloud", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public Task<OrganizeResult> OrganizeAsync(
        string sourceDirectory,
        string destinationDirectory,
        IProgress<OrganizeProgress> progress,
        CancellationToken cancellationToken,
        bool skipCloudOnlyFiles = true)
    {
        return OrganizeAsync(
            sourceDirectory,
            destinationDirectory,
            progress,
            cancellationToken,
            skipCloudOnlyFiles ? CloudFileHandlingMode.Skip : CloudFileHandlingMode.Download);
    }

    public async Task<OrganizeResult> OrganizeAsync(
        string sourceDirectory,
        string destinationDirectory,
        IProgress<OrganizeProgress> progress,
        CancellationToken cancellationToken,
        CloudFileHandlingMode cloudFileMode)
    {
        var strings = LocalizationService.Strings;
        var stopwatch = Stopwatch.StartNew();
        int copiedCount = 0;
        int skippedCount = 0;
        int errorCount = 0;
        int fallbackCount = 0;

        try
        {
            if (!System.IO.Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException($"ソースフォルダが見つかりません: {sourceDirectory}");
            }

            if (!System.IO.Directory.Exists(destinationDirectory))
            {
                System.IO.Directory.CreateDirectory(destinationDirectory);
            }

            // Phase 1: Scan files
            progress.Report(new OrganizeProgress
            {
                Phase = OrganizePhase.Scanning,
                StatusMessage = strings.StatusScanning,
                NewLogEntry = new LogEntry
                {
                    Level = LogLevel.Info,
                    Message = string.Format(strings.LogScanningStartedFormat, sourceDirectory)
                }
            });

            var files = await Task.Run(() =>
            {
                return System.IO.Directory.EnumerateFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(file => SupportedExtensions.Contains(Path.GetExtension(file)))
                    .ToList();
            }, cancellationToken);

            int totalFiles = files.Count;

            progress.Report(new OrganizeProgress
            {
                Phase = OrganizePhase.Organizing,
                TotalCount = totalFiles,
                ProcessedCount = 0,
                StatusMessage = string.Format(strings.StatusScanCompletedFormat, totalFiles),
                NewLogEntry = new LogEntry
                {
                    Level = LogLevel.Info,
                    Message = string.Format(strings.LogScanCompletedFormat, totalFiles)
                }
            });

            if (totalFiles == 0)
            {
                stopwatch.Stop();
                return new OrganizeResult
                {
                    TotalScanned = 0,
                    CopiedCount = 0,
                    SkippedCount = 0,
                    ErrorCount = 0,
                    FallbackCount = 0,
                    Duration = stopwatch.Elapsed,
                    IsCancelled = false
                };
            }

            // Phase 2: Process and Organize Files
            for (int i = 0; i < files.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sourceFile = files[i];
                var fileName = Path.GetFileName(sourceFile);
                int currentIndex = i + 1;

                try
                {
                    // 0. クラウド専用ファイル（未ダウンロード）の事前チェックと必要に応じた一時ダウンロード
                    bool isOriginallyCloudOnly = _cloudFileService.IsCloudOnlyFile(sourceFile);

                    if (isOriginallyCloudOnly)
                    {
                        if (cloudFileMode == CloudFileHandlingMode.Skip)
                        {
                            skippedCount++;
                            progress.Report(new OrganizeProgress
                            {
                                Phase = OrganizePhase.Organizing,
                                ProcessedCount = currentIndex,
                                TotalCount = totalFiles,
                                CopiedCount = copiedCount,
                                SkippedCount = skippedCount,
                                ErrorCount = errorCount,
                                FallbackCount = fallbackCount,
                                CurrentFilePath = sourceFile,
                                StatusMessage = string.Format(strings.StatusCloudSkipFormat, fileName),
                                NewLogEntry = new LogEntry
                                {
                                    Level = LogLevel.Warning,
                                    Message = string.Format(strings.LogCloudSkipFormat, fileName),
                                    FilePath = sourceFile
                                }
                            });
                            continue;
                        }

                        // 一時ダウンロード (Hydrate)
                        progress.Report(new OrganizeProgress
                        {
                            Phase = OrganizePhase.Organizing,
                            ProcessedCount = currentIndex,
                            TotalCount = totalFiles,
                            CopiedCount = copiedCount,
                            SkippedCount = skippedCount,
                            ErrorCount = errorCount,
                            FallbackCount = fallbackCount,
                            CurrentFilePath = sourceFile,
                            StatusMessage = string.Format(strings.StatusDownloadingFormat, fileName),
                            NewLogEntry = new LogEntry
                            {
                                Level = LogLevel.Info,
                                Message = string.Format(strings.LogDownloadingFormat, fileName),
                                FilePath = sourceFile
                            }
                        });

                        bool downloadSuccess = await _cloudFileService.HydrateFileAsync(sourceFile, cancellationToken);
                        if (!downloadSuccess)
                        {
                            errorCount++;
                            progress.Report(new OrganizeProgress
                            {
                                Phase = OrganizePhase.Organizing,
                                ProcessedCount = currentIndex,
                                TotalCount = totalFiles,
                                CopiedCount = copiedCount,
                                SkippedCount = skippedCount,
                                ErrorCount = errorCount,
                                FallbackCount = fallbackCount,
                                CurrentFilePath = sourceFile,
                                StatusMessage = string.Format(strings.StatusDownloadErrorFormat, fileName),
                                NewLogEntry = new LogEntry
                                {
                                    Level = LogLevel.Error,
                                    Message = string.Format(strings.LogDownloadErrorFormat, fileName),
                                    FilePath = sourceFile
                                }
                            });
                            continue;
                        }
                    }

                    // 1. Extract Date
                    var (captureDate, dateSource, detailInfo) = ExtractDateTaken(sourceFile);

                    bool isFallback = dateSource is DateSourceType.FilenamePattern or DateSourceType.FileCreationTime or DateSourceType.FileModifiedTime;
                    if (isFallback)
                    {
                        fallbackCount++;
                    }

                    // 2. Build Destination Path: [Dest] \ YYYY \ YYYY-MM \ YYYY-MM-DD (\ 動画) \ [FileName]
                    string year = captureDate.ToString("yyyy", CultureInfo.InvariantCulture);
                    string yearMonth = captureDate.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                    string yearMonthDay = captureDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                    string targetFolder = Path.Combine(destinationDirectory, year, yearMonth, yearMonthDay);
                    if (IsVideoFile(sourceFile))
                    {
                        targetFolder = Path.Combine(targetFolder, "動画");
                    }

                    if (!System.IO.Directory.Exists(targetFolder))
                    {
                        System.IO.Directory.CreateDirectory(targetFolder);
                    }

                    // 3. Resolve File Name Conflicts
                    var (targetFilePath, isDuplicate) = ResolveTargetFilePath(sourceFile, targetFolder);

                    if (isDuplicate)
                    {
                        skippedCount++;
                        progress.Report(new OrganizeProgress
                        {
                            Phase = OrganizePhase.Organizing,
                            ProcessedCount = currentIndex,
                            TotalCount = totalFiles,
                            CopiedCount = copiedCount,
                            SkippedCount = skippedCount,
                            ErrorCount = errorCount,
                            FallbackCount = fallbackCount,
                            CurrentFilePath = sourceFile,
                            StatusMessage = string.Format(strings.StatusDuplicateSkipFormat, fileName),
                            NewLogEntry = new LogEntry
                            {
                                Level = LogLevel.Warning,
                                Message = string.Format(strings.LogDuplicateSkipFormat, fileName, Path.GetRelativePath(destinationDirectory, targetFilePath)),
                                FilePath = sourceFile
                            }
                        });
                    }
                    else
                    {
                        // Copy file safely
                        File.Copy(sourceFile, targetFilePath, overwrite: false);
                        copiedCount++;

                        var (logLevel, note) = dateSource switch
                        {
                            DateSourceType.Exif => (LogLevel.Success, string.Format(strings.NoteExifFormat, captureDate)),
                            DateSourceType.QuickTime => (LogLevel.Success, string.Format(strings.NoteVideoFormat, captureDate)),
                            DateSourceType.FilenamePattern => (LogLevel.Warning, string.Format(strings.NoteFilenameFallbackFormat, captureDate)),
                            DateSourceType.FileModifiedTime => (LogLevel.Warning, string.Format(strings.NoteModifiedFallbackFormat, captureDate)),
                            _ => (LogLevel.Warning, string.Format(strings.NoteCreationFallbackFormat, captureDate))
                        };

                        progress.Report(new OrganizeProgress
                        {
                            Phase = OrganizePhase.Organizing,
                            ProcessedCount = currentIndex,
                            TotalCount = totalFiles,
                            CopiedCount = copiedCount,
                            SkippedCount = skippedCount,
                            ErrorCount = errorCount,
                            FallbackCount = fallbackCount,
                            CurrentFilePath = sourceFile,
                            StatusMessage = string.Format(strings.StatusCopiedFormat, fileName),
                            NewLogEntry = new LogEntry
                            {
                                Level = logLevel,
                                Message = string.Format(strings.LogCopiedFormat, fileName, Path.GetRelativePath(destinationDirectory, targetFilePath), note),
                                FilePath = sourceFile
                            }
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    string errorMsg = IsCloudFileException(ex)
                        ? string.Format(strings.LogCloudAccessErrorFormat, fileName)
                        : string.Format(strings.LogGenericErrorFormat, fileName, ex.Message);

                    progress.Report(new OrganizeProgress
                    {
                        Phase = OrganizePhase.Organizing,
                        ProcessedCount = currentIndex,
                        TotalCount = totalFiles,
                        CopiedCount = copiedCount,
                        SkippedCount = skippedCount,
                        ErrorCount = errorCount,
                        FallbackCount = fallbackCount,
                        CurrentFilePath = sourceFile,
                        StatusMessage = string.Format(strings.StatusErrorFormat, fileName),
                        NewLogEntry = new LogEntry
                        {
                            Level = LogLevel.Error,
                            Message = errorMsg,
                            FilePath = sourceFile
                        }
                    });
                }
            }

            stopwatch.Stop();

            string completionSummary = fallbackCount > 0
                ? string.Format(strings.LogCompletionSummaryWithFallbackFormat, totalFiles, copiedCount, skippedCount, errorCount, fallbackCount, stopwatch.Elapsed.ToString(@"mm\:ss"))
                : string.Format(strings.LogCompletionSummaryFormat, totalFiles, copiedCount, skippedCount, errorCount, stopwatch.Elapsed.ToString(@"mm\:ss"));

            progress.Report(new OrganizeProgress
            {
                Phase = OrganizePhase.Completed,
                ProcessedCount = totalFiles,
                TotalCount = totalFiles,
                CopiedCount = copiedCount,
                SkippedCount = skippedCount,
                ErrorCount = errorCount,
                FallbackCount = fallbackCount,
                StatusMessage = completionSummary,
                NewLogEntry = new LogEntry
                {
                    Level = fallbackCount > 0 ? LogLevel.Warning : LogLevel.Info,
                    Message = completionSummary
                }
            });

            return new OrganizeResult
            {
                TotalScanned = totalFiles,
                CopiedCount = copiedCount,
                SkippedCount = skippedCount,
                ErrorCount = errorCount,
                FallbackCount = fallbackCount,
                Duration = stopwatch.Elapsed,
                IsCancelled = false
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            progress.Report(new OrganizeProgress
            {
                Phase = OrganizePhase.Cancelled,
                CopiedCount = copiedCount,
                SkippedCount = skippedCount,
                ErrorCount = errorCount,
                FallbackCount = fallbackCount,
                StatusMessage = "処理がユーザーによってキャンセルされました。",
                NewLogEntry = new LogEntry
                {
                    Level = LogLevel.Warning,
                    Message = $"処理が中断されました。(コピー済み: {copiedCount} 件, スキップ: {skippedCount} 件)"
                }
            });

            return new OrganizeResult
            {
                CopiedCount = copiedCount,
                SkippedCount = skippedCount,
                ErrorCount = errorCount,
                FallbackCount = fallbackCount,
                Duration = stopwatch.Elapsed,
                IsCancelled = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            progress.Report(new OrganizeProgress
            {
                Phase = OrganizePhase.Failed,
                CopiedCount = copiedCount,
                SkippedCount = skippedCount,
                ErrorCount = errorCount,
                FallbackCount = fallbackCount,
                StatusMessage = $"重大なエラーが発生しました: {ex.Message}",
                NewLogEntry = new LogEntry
                {
                    Level = LogLevel.Error,
                    Message = $"重大なエラー: {ex.Message}"
                }
            });

            return new OrganizeResult
            {
                CopiedCount = copiedCount,
                SkippedCount = skippedCount,
                ErrorCount = errorCount,
                FallbackCount = fallbackCount,
                Duration = stopwatch.Elapsed,
                IsCancelled = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public enum DateSourceType
    {
        Exif,
        QuickTime,
        FilenamePattern,
        FileModifiedTime,
        FileCreationTime
    }

    /// <summary>
    /// Extracts capture date strictly following:
    /// 1. Exif DateTimeOriginal / DateTime / DateTimeDigitized (with TryGetDateTime + Multi-format string parse)
    /// 2. GPS DateTimeStamp
    /// 3. QuickTime/MP4 TagCreated
    /// 4. Filename Pattern matching (e.g. 20230308, IMG_20230308_..., Unix timestamp)
    /// 5. Fallback to earliest valid file system timestamp (LastWriteTime or CreationTime)
    /// </summary>
    public static (DateTime Date, DateSourceType Source, string Detail) ExtractDateTaken(string filePath)
    {
        // 1. Try MetadataExtractor
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(filePath);

            // 1a. Exif SubIFD
            var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfdDirectory != null)
            {
                if (TryGetExifDate(subIfdDirectory, ExifDirectoryBase.TagDateTimeOriginal, out var dto))
                {
                    return (EnsureLocalTime(dto), DateSourceType.Exif, "ExifSubIFD:DateTimeOriginal");
                }

                if (TryGetExifDate(subIfdDirectory, ExifDirectoryBase.TagDateTimeDigitized, out var dtd))
                {
                    return (EnsureLocalTime(dtd), DateSourceType.Exif, "ExifSubIFD:DateTimeDigitized");
                }
            }

            // 1b. Exif IFD0
            var ifd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (ifd0Directory != null)
            {
                if (TryGetExifDate(ifd0Directory, ExifDirectoryBase.TagDateTime, out var dt))
                {
                    return (EnsureLocalTime(dt), DateSourceType.Exif, "ExifIFD0:DateTime");
                }
            }

            // 1c. GPS Directory
            var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
            if (gpsDirectory != null)
            {
                if (TryGetExifDate(gpsDirectory, GpsDirectory.TagDateStamp, out var gpsDate))
                {
                    return (EnsureLocalTime(gpsDate), DateSourceType.Exif, "GPS:DateStamp");
                }
            }

            // 1d. QuickTime Movie Header / Track Header
            var qtMovieHeader = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
            if (qtMovieHeader != null)
            {
                if (qtMovieHeader.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out var qtCreated) && IsValidDate(qtCreated))
                {
                    return (EnsureLocalTime(qtCreated), DateSourceType.QuickTime, "QuickTime:MovieHeaderCreated");
                }
            }

            var qtTrackHeader = directories.OfType<QuickTimeTrackHeaderDirectory>().FirstOrDefault();
            if (qtTrackHeader != null)
            {
                if (qtTrackHeader.TryGetDateTime(QuickTimeTrackHeaderDirectory.TagCreated, out var qtTrackCreated) && IsValidDate(qtTrackCreated))
                {
                    return (EnsureLocalTime(qtTrackCreated), DateSourceType.QuickTime, "QuickTime:TrackHeaderCreated");
                }
            }
        }
        catch
        {
            // Ignore metadata extraction errors and proceed to filename/filesystem detection
        }

        // 2. Try Filename pattern detection (e.g. 20230308, IMG_20230308_123456, LINE_ALBUM_..., Unix timestamp)
        var filename = Path.GetFileNameWithoutExtension(filePath);
        if (TryExtractDateFromFilename(filename, out var filenameDate))
        {
            return (filenameDate, DateSourceType.FilenamePattern, "Filename:Pattern");
        }

        // 3. Fallback to File system timestamps (prefer earlier of LastWriteTime and CreationTime)
        var creationTime = File.GetCreationTime(filePath);
        var writeTime = File.GetLastWriteTime(filePath);

        bool isValidCreation = IsValidDate(creationTime);
        bool isValidWrite = IsValidDate(writeTime);

        if (isValidCreation && isValidWrite)
        {
            // If LastWriteTime is significantly earlier than CreationTime, it is typically the preserved camera file timestamp
            if (writeTime < creationTime)
            {
                return (writeTime, DateSourceType.FileModifiedTime, "FileSystem:LastWriteTime");
            }
            return (creationTime, DateSourceType.FileCreationTime, "FileSystem:CreationTime");
        }

        if (isValidCreation)
        {
            return (creationTime, DateSourceType.FileCreationTime, "FileSystem:CreationTime");
        }

        if (isValidWrite)
        {
            return (writeTime, DateSourceType.FileModifiedTime, "FileSystem:LastWriteTime");
        }

        return (DateTime.Now, DateSourceType.FileCreationTime, "Fallback:Now");
    }

    private static bool TryGetExifDate(MetadataExtractor.Directory dir, int tagType, out DateTime date)
    {
        date = default;

        // Try built-in helper first
        if (dir.TryGetDateTime(tagType, out date) && IsValidDate(date))
        {
            return true;
        }

        // Try string extraction and custom parsing
        var dateStr = dir.GetString(tagType);
        if (!string.IsNullOrWhiteSpace(dateStr))
        {
            dateStr = dateStr.Trim();
            foreach (var format in ExifDateTimeFormats)
            {
                if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) && IsValidDate(date))
                {
                    return true;
                }
            }

            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) && IsValidDate(date))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryExtractDateFromFilename(string filename, out DateTime resultDate)
    {
        resultDate = default;
        if (string.IsNullOrWhiteSpace(filename))
        {
            return false;
        }

        // Match standard date pattern (e.g. 20230308, 2023-03-08, 2023_03_08, IMG_20230308_143000)
        var match = FilenameDateRegex.Match(filename);
        if (match.Success)
        {
            if (int.TryParse(match.Groups["year"].Value, out int year) &&
                int.TryParse(match.Groups["month"].Value, out int month) &&
                int.TryParse(match.Groups["day"].Value, out int day))
            {
                int hour = 0, min = 0, sec = 0;
                if (match.Groups["hour"].Success) int.TryParse(match.Groups["hour"].Value, out hour);
                if (match.Groups["min"].Success) int.TryParse(match.Groups["min"].Value, out min);
                if (match.Groups["sec"].Success) int.TryParse(match.Groups["sec"].Value, out sec);

                try
                {
                    var dt = new DateTime(year, month, day, hour, min, sec, DateTimeKind.Local);
                    if (IsValidDate(dt))
                    {
                        resultDate = dt;
                        return true;
                    }
                }
                catch
                {
                    // Invalid date numbers (e.g. Feb 30)
                }
            }
        }

        // Match Unix timestamp in milliseconds or seconds (e.g., LINE downloads, Android camera)
        var tsMatch = UnixTimestampRegex.Match(filename);
        if (tsMatch.Success)
        {
            var tsStr = tsMatch.Groups["ts"].Value;
            if (long.TryParse(tsStr, out long ts))
            {
                try
                {
                    DateTimeOffset dto;
                    if (tsStr.Length == 13) // ms
                    {
                        dto = DateTimeOffset.FromUnixTimeMilliseconds(ts);
                    }
                    else if (tsStr.Length == 10) // s
                    {
                        dto = DateTimeOffset.FromUnixTimeSeconds(ts);
                    }
                    else
                    {
                        return false;
                    }

                    var localDt = dto.LocalDateTime;
                    if (IsValidDate(localDt))
                    {
                        resultDate = localDt;
                        return true;
                    }
                }
                catch
                {
                    // Ignore timestamp conversion errors
                }
            }
        }

        return false;
    }

    private static bool IsValidDate(DateTime dt)
    {
        return dt.Year >= 1990 && dt.Year <= DateTime.Now.Year + 1;
    }

    private static DateTime EnsureLocalTime(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc)
        {
            return dt.ToLocalTime();
        }
        return dt;
    }

    /// <summary>
    /// Resolves file collision:
    /// - If target file does not exist: returns target path, isDuplicate=false
    /// - If target file exists: compares size & MD5 hash.
    ///   - If content matches: returns existing target path, isDuplicate=true
    ///   - If content differs: searches for suffixed names (file_1.jpg, file_2.jpg).
    ///     If one of suffixed names has matching content, isDuplicate=true.
    ///     Otherwise returns the next available non-existent suffixed file path.
    /// </summary>
    private static (string TargetFilePath, bool IsDuplicate) ResolveTargetFilePath(string sourceFilePath, string targetFolder)
    {
        var originalFileNameWithoutExt = Path.GetFileNameWithoutExtension(sourceFilePath);
        var extension = Path.GetExtension(sourceFilePath);
        var directTarget = Path.Combine(targetFolder, Path.GetFileName(sourceFilePath));

        if (!File.Exists(directTarget))
        {
            return (directTarget, false);
        }

        // Compare direct target
        if (AreFilesIdentical(sourceFilePath, directTarget))
        {
            return (directTarget, true);
        }

        // Suffix loop: _1, _2, _3, ...
        int suffix = 1;
        while (true)
        {
            var suffixedFileName = $"{originalFileNameWithoutExt}_{suffix}{extension}";
            var suffixedTarget = Path.Combine(targetFolder, suffixedFileName);

            if (!File.Exists(suffixedTarget))
            {
                return (suffixedTarget, false);
            }

            if (AreFilesIdentical(sourceFilePath, suffixedTarget))
            {
                return (suffixedTarget, true);
            }

            suffix++;
        }
    }

    private static bool AreFilesIdentical(string file1, string file2)
    {
        var fi1 = new FileInfo(file1);
        var fi2 = new FileInfo(file2);

        if (fi1.Length != fi2.Length)
        {
            return false;
        }

        // 出力先ファイルがオンライン専用プレースホルダーで、サイズおよび更新日時が同一の場合は同一と判定（無駄なダウンロードを回避）
        if (IsCloudOnlyFile(file2) && fi1.LastWriteTimeUtc == fi2.LastWriteTimeUtc)
        {
            return true;
        }

        // Compare MD5 checksums for files with same length
        using var md5 = MD5.Create();
        using var stream1 = new FileStream(file1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var stream2 = new FileStream(file2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        byte[] hash1 = md5.ComputeHash(stream1);
        byte[] hash2 = md5.ComputeHash(stream2);

        return hash1.AsSpan().SequenceEqual(hash2);
    }
}

