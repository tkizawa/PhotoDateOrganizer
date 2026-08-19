using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhotoDateOrganizer.Models;
using PhotoDateOrganizer.Services;
using Xunit;

namespace PhotoDateOrganizer.Tests;

public class PhotoOrganizerServiceTests : IDisposable
{
    private readonly string _testTempDir;
    private readonly string _sourceDir;
    private readonly string _destDir;
    private readonly PhotoOrganizerService _service;

    public PhotoOrganizerServiceTests()
    {
        _testTempDir = Path.Combine(Path.GetTempPath(), "PhotoDateOrganizerTests_" + Guid.NewGuid().ToString("N"));
        _sourceDir = Path.Combine(_testTempDir, "Source");
        _destDir = Path.Combine(_testTempDir, "Destination");

        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_destDir);

        _service = new PhotoOrganizerService();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testTempDir))
            {
                Directory.Delete(_testTempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    [Fact]
    public async Task OrganizeAsync_EmptyFolder_ReturnsZeroCount()
    {
        var progress = new Progress<OrganizeProgress>(_ => { });
        var result = await _service.OrganizeAsync(_sourceDir, _destDir, progress, CancellationToken.None);

        Assert.Equal(0, result.TotalScanned);
        Assert.Equal(0, result.CopiedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(0, result.FallbackCount);
        Assert.False(result.IsCancelled);
    }

    [Fact]
    public async Task OrganizeAsync_StandardFiles_CreatesCorrectFolderHierarchy()
    {
        // Create dummy source image file with date in filename
        var sampleFile = Path.Combine(_sourceDir, "IMG_20230308_143000.jpg");
        await File.WriteAllBytesAsync(sampleFile, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 });

        var progress = new Progress<OrganizeProgress>(_ => { });
        var result = await _service.OrganizeAsync(_sourceDir, _destDir, progress, CancellationToken.None);

        Assert.Equal(1, result.TotalScanned);
        Assert.Equal(1, result.CopiedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.ErrorCount);

        // Verify folder structure exists: 2023 / 2023-03 / 2023-03-08 / IMG_20230308_143000.jpg
        var expectedPath = Path.Combine(_destDir, "2023", "2023-03", "2023-03-08", "IMG_20230308_143000.jpg");
        Assert.True(File.Exists(expectedPath), $"Expected file at {expectedPath}");
    }

    [Fact]
    public async Task OrganizeAsync_VideoFiles_SavesToVideosSubdirectory()
    {
        var photoFile = Path.Combine(_sourceDir, "IMG_20230308_143000.jpg");
        var videoFileMp4 = Path.Combine(_sourceDir, "VID_20230308_150000.mp4");
        var videoFileMov = Path.Combine(_sourceDir, "MOV_20230308_160000.mov");

        await File.WriteAllBytesAsync(photoFile, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        await File.WriteAllBytesAsync(videoFileMp4, new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 });
        await File.WriteAllBytesAsync(videoFileMov, new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70 });

        var progress = new Progress<OrganizeProgress>(_ => { });
        var result = await _service.OrganizeAsync(_sourceDir, _destDir, progress, CancellationToken.None);

        Assert.Equal(3, result.TotalScanned);
        Assert.Equal(3, result.CopiedCount);

        var expectedPhotoPath = Path.Combine(_destDir, "2023", "2023-03", "2023-03-08", "IMG_20230308_143000.jpg");
        var expectedMp4Path = Path.Combine(_destDir, "2023", "2023-03", "2023-03-08", "動画", "VID_20230308_150000.mp4");
        var expectedMovPath = Path.Combine(_destDir, "2023", "2023-03", "2023-03-08", "動画", "MOV_20230308_160000.mov");

        Assert.True(File.Exists(expectedPhotoPath), $"Expected photo at {expectedPhotoPath}");
        Assert.True(File.Exists(expectedMp4Path), $"Expected video at {expectedMp4Path}");
        Assert.True(File.Exists(expectedMovPath), $"Expected video at {expectedMovPath}");
    }

    [Theory]
    [InlineData("video.mp4", true)]
    [InlineData("video.MOV", true)]
    [InlineData("photo.jpg", false)]
    [InlineData("photo.PNG", false)]
    [InlineData("photo.heic", false)]
    public void IsVideoFile_ChecksExtensionsCorrectly(string fileName, bool expectedIsVideo)
    {
        Assert.Equal(expectedIsVideo, PhotoOrganizerService.IsVideoFile(fileName));
    }

    [Theory]
    [InlineData("IMG_20230308_123456", 2023, 3, 8)]
    [InlineData("2023-03-08_Photo", 2023, 3, 8)]
    [InlineData("LINE_ALBUM_20230308_230308", 2023, 3, 8)]
    [InlineData("Screenshot_2023.03.08-15.30.00", 2023, 3, 8)]
    [InlineData("20230308", 2023, 3, 8)]
    public void TryExtractDateFromFilename_ValidPatterns_ReturnsCorrectDate(string filename, int expectedYear, int expectedMonth, int expectedDay)
    {
        bool success = PhotoOrganizerService.TryExtractDateFromFilename(filename, out var extractedDate);
        Assert.True(success);
        Assert.Equal(expectedYear, extractedDate.Year);
        Assert.Equal(expectedMonth, extractedDate.Month);
        Assert.Equal(expectedDay, extractedDate.Day);
    }

    [Fact]
    public async Task OrganizeAsync_DuplicateIdenticalFiles_SkipsCopy()
    {
        var sampleFile1 = Path.Combine(_sourceDir, "photo.jpg");
        var subDir = Path.Combine(_sourceDir, "SubFolder");
        Directory.CreateDirectory(subDir);
        var sampleFile2 = Path.Combine(subDir, "photo.jpg");

        byte[] identicalContent = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        await File.WriteAllBytesAsync(sampleFile1, identicalContent);
        await File.WriteAllBytesAsync(sampleFile2, identicalContent);

        var progress = new Progress<OrganizeProgress>(_ => { });
        var result = await _service.OrganizeAsync(_sourceDir, _destDir, progress, CancellationToken.None);

        Assert.Equal(2, result.TotalScanned);
        Assert.Equal(1, result.CopiedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public async Task OrganizeAsync_DifferentFilesWithSameName_AppendsSuffix()
    {
        var sampleFile1 = Path.Combine(_sourceDir, "photo.jpg");
        var subDir = Path.Combine(_sourceDir, "SubFolder");
        Directory.CreateDirectory(subDir);
        var sampleFile2 = Path.Combine(subDir, "photo.jpg");

        // Different file contents
        await File.WriteAllBytesAsync(sampleFile1, new byte[] { 1, 2, 3, 4, 5 });
        await File.WriteAllBytesAsync(sampleFile2, new byte[] { 6, 7, 8, 9, 10 });

        var progress = new Progress<OrganizeProgress>(_ => { });
        var result = await _service.OrganizeAsync(_sourceDir, _destDir, progress, CancellationToken.None);

        Assert.Equal(2, result.TotalScanned);
        Assert.Equal(2, result.CopiedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.ErrorCount);

        var destFiles = Directory.GetFiles(_destDir, "*.*", SearchOption.AllDirectories)
                                 .Select(Path.GetFileName)
                                 .OrderBy(x => x)
                                 .ToList();

        Assert.Contains("photo.jpg", destFiles);
        Assert.Contains("photo_1.jpg", destFiles);
    }

    [Fact]
    public async Task OrganizeAsync_CancellationRequested_CancelsGracefully()
    {
        for (int i = 0; i < 20; i++)
        {
            await File.WriteAllBytesAsync(Path.Combine(_sourceDir, $"img_{i}.png"), new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        }

        var cts = new CancellationTokenSource();
        int progressCalls = 0;
        var progress = new Progress<OrganizeProgress>(p =>
        {
            progressCalls++;
            if (progressCalls >= 2)
            {
                cts.Cancel();
            }
        });

        var result = await _service.OrganizeAsync(_sourceDir, _destDir, progress, cts.Token);

        Assert.True(result.IsCancelled);
        Assert.True(result.CopiedCount < 20);
    }

    [Fact]
    public void IsCloudOnlyFile_NonExistentOrLocalFile_ReturnsFalse()
    {
        Assert.False(PhotoOrganizerService.IsCloudOnlyFile(""));
        Assert.False(PhotoOrganizerService.IsCloudOnlyFile(Path.Combine(_sourceDir, "non_existent.jpg")));

        var localFile = Path.Combine(_sourceDir, "local_photo.jpg");
        File.WriteAllBytes(localFile, new byte[] { 1, 2, 3 });

        Assert.False(PhotoOrganizerService.IsCloudOnlyFile(localFile));
    }

    [Fact]
    public void IsCloudOnlyFile_OfflineAttribute_ReturnsTrue()
    {
        var cloudFile = Path.Combine(_sourceDir, "cloud_photo.jpg");
        File.WriteAllBytes(cloudFile, new byte[] { 1, 2, 3 });

        // Set Offline file attribute
        File.SetAttributes(cloudFile, File.GetAttributes(cloudFile) | FileAttributes.Offline);

        Assert.True(PhotoOrganizerService.IsCloudOnlyFile(cloudFile));
    }

    [Fact]
    public void IsCloudFileException_DetectsKnownHResultAndMessages()
    {
        var cantAccessEx = new IOException("File error", unchecked((int)0x80070780));
        var cloudUnsuccessfulEx = new IOException("Cloud error", unchecked((int)0x800701AA));
        var genericMessageEx = new Exception("クラウド ファイル プロバイダーが実行されていません。");
        var regularEx = new FileNotFoundException("File not found");

        Assert.True(PhotoOrganizerService.IsCloudFileException(cantAccessEx));
        Assert.True(PhotoOrganizerService.IsCloudFileException(cloudUnsuccessfulEx));
        Assert.True(PhotoOrganizerService.IsCloudFileException(genericMessageEx));
        Assert.False(PhotoOrganizerService.IsCloudFileException(regularEx));
    }

    [Fact]
    public async Task OrganizeAsync_SkipCloudOnlyFiles_SkipsOfflineFiles()
    {
        var localFile = Path.Combine(_sourceDir, "IMG_20230308_100000.jpg");
        var cloudFile = Path.Combine(_sourceDir, "IMG_20230308_200000.jpg");

        await File.WriteAllBytesAsync(localFile, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        await File.WriteAllBytesAsync(cloudFile, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        File.SetAttributes(cloudFile, File.GetAttributes(cloudFile) | FileAttributes.Offline);

        var progress = new Progress<OrganizeProgress>(_ => { });
        var result = await _service.OrganizeAsync(_sourceDir, _destDir, progress, CancellationToken.None, skipCloudOnlyFiles: true);

        Assert.Equal(2, result.TotalScanned);
        Assert.Equal(1, result.CopiedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, result.ErrorCount);

        var destFiles = Directory.GetFiles(_destDir, "*.*", SearchOption.AllDirectories);
        Assert.Single(destFiles);
        Assert.Equal("IMG_20230308_100000.jpg", Path.GetFileName(destFiles[0]));
    }
}
