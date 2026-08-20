using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PhotoDateOrganizer.Models;
using PhotoDateOrganizer.Services;
using Xunit;

namespace PhotoDateOrganizer.Tests;

public class CloudFileServiceTests : IDisposable
{
    private readonly string _testTempDir;
    private readonly string _sourceDir;
    private readonly string _destDir;

    public CloudFileServiceTests()
    {
        _testTempDir = Path.Combine(Path.GetTempPath(), "PhotoDateOrganizer_CloudTests_" + Guid.NewGuid().ToString("N"));
        _sourceDir = Path.Combine(_testTempDir, "Source");
        _destDir = Path.Combine(_testTempDir, "Destination");

        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_destDir);
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
            // Ignore cleanup errors
        }
    }

    private class FakeCloudFileService : ICloudFileService
    {
        public bool IsCloudOnly { get; set; }
        public bool HydrateResult { get; set; } = true;
        public bool DehydrateResult { get; set; } = true;
        public int HydrateCallCount { get; private set; }
        public int DehydrateCallCount { get; private set; }

        public bool IsCloudOnlyFile(string filePath) => IsCloudOnly;

        public Task<bool> HydrateFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            HydrateCallCount++;
            return Task.FromResult(HydrateResult);
        }

        public bool DehydrateFile(string filePath, out string? errorMessage)
        {
            DehydrateCallCount++;
            errorMessage = DehydrateResult ? null : "Dehydrate simulated failure";
            return DehydrateResult;
        }

        public Task<(bool success, string? errorMessage)> DehydrateFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            DehydrateCallCount++;
            string? errorMessage = DehydrateResult ? null : "Dehydrate simulated failure";
            return Task.FromResult((DehydrateResult, errorMessage));
        }
    }


    [Fact]
    public async Task OrganizeAsync_CloudOnlyFile_WithSkipMode_ShouldSkip()
    {
        // Arrange
        var fakeCloudService = new FakeCloudFileService { IsCloudOnly = true };
        var service = new PhotoOrganizerService(fakeCloudService);

        var sampleFile = Path.Combine(_sourceDir, "IMG_20230308_143000.jpg");
        await File.WriteAllBytesAsync(sampleFile, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var progress = new Progress<OrganizeProgress>(_ => { });

        // Act
        var result = await service.OrganizeAsync(_sourceDir, _destDir, progress, CancellationToken.None, CloudFileHandlingMode.Skip);

        // Assert
        Assert.Equal(1, result.TotalScanned);
        Assert.Equal(0, result.CopiedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, fakeCloudService.HydrateCallCount);
        Assert.Equal(0, fakeCloudService.DehydrateCallCount);
    }

    [Fact]
    public async Task OrganizeAsync_CloudOnlyFile_WithHydrateAndDehydrate_ShouldHydrateAndDehydrate()
    {
        // Arrange
        var fakeCloudService = new FakeCloudFileService { IsCloudOnly = true, HydrateResult = true, DehydrateResult = true };
        var service = new PhotoOrganizerService(fakeCloudService);

        var sampleFile = Path.Combine(_sourceDir, "IMG_20230308_143000.jpg");
        await File.WriteAllBytesAsync(sampleFile, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var progress = new Progress<OrganizeProgress>(_ => { });

        // Act
        var result = await service.OrganizeAsync(_sourceDir, _destDir, progress, CancellationToken.None, CloudFileHandlingMode.HydrateAndDehydrate);

        // Assert
        Assert.Equal(1, result.TotalScanned);
        Assert.Equal(1, result.CopiedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(1, fakeCloudService.HydrateCallCount);
        Assert.Equal(1, fakeCloudService.DehydrateCallCount);

        var expectedPath = Path.Combine(_destDir, "2023", "2023-03", "2023-03-08", "IMG_20230308_143000.jpg");
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public async Task OrganizeAsync_CloudOnlyFile_WithDownloadAndKeep_ShouldHydrateButNotDehydrate()
    {
        // Arrange
        var fakeCloudService = new FakeCloudFileService { IsCloudOnly = true, HydrateResult = true };
        var service = new PhotoOrganizerService(fakeCloudService);

        var sampleFile = Path.Combine(_sourceDir, "IMG_20230308_143000.jpg");
        await File.WriteAllBytesAsync(sampleFile, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var progress = new Progress<OrganizeProgress>(_ => { });

        // Act
        var result = await service.OrganizeAsync(_sourceDir, _destDir, progress, CancellationToken.None, CloudFileHandlingMode.DownloadAndKeep);

        // Assert
        Assert.Equal(1, result.TotalScanned);
        Assert.Equal(1, result.CopiedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(1, fakeCloudService.HydrateCallCount);
        Assert.Equal(0, fakeCloudService.DehydrateCallCount);
    }

    [Fact]
    public async Task OrganizeAsync_MultipleCloudOnlyFiles_ShouldProcessAllSequentially()
    {
        // Arrange
        var fakeCloudService = new FakeCloudFileService { IsCloudOnly = true, HydrateResult = true, DehydrateResult = true };
        var service = new PhotoOrganizerService(fakeCloudService);

        var file1 = Path.Combine(_sourceDir, "IMG_20230308_143000.jpg");
        var file2 = Path.Combine(_sourceDir, "IMG_20230309_150000.jpg");
        var file3 = Path.Combine(_sourceDir, "IMG_20230310_160000.jpg");
        await File.WriteAllBytesAsync(file1, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        await File.WriteAllBytesAsync(file2, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        await File.WriteAllBytesAsync(file3, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var progress = new Progress<OrganizeProgress>(_ => { });

        // Act
        var result = await service.OrganizeAsync(_sourceDir, _destDir, progress, CancellationToken.None, CloudFileHandlingMode.HydrateAndDehydrate);

        // Assert
        Assert.Equal(3, result.TotalScanned);
        Assert.Equal(3, result.CopiedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(3, fakeCloudService.HydrateCallCount);
        Assert.Equal(3, fakeCloudService.DehydrateCallCount);

        Assert.True(File.Exists(Path.Combine(_destDir, "2023", "2023-03", "2023-03-08", "IMG_20230308_143000.jpg")));
        Assert.True(File.Exists(Path.Combine(_destDir, "2023", "2023-03", "2023-03-09", "IMG_20230309_150000.jpg")));
        Assert.True(File.Exists(Path.Combine(_destDir, "2023", "2023-03", "2023-03-10", "IMG_20230310_160000.jpg")));
    }


    [Fact]
    public async Task OrganizeAsync_CloudOnlyFile_HydrateFailure_ShouldCountAsError()
    {
        // Arrange
        var fakeCloudService = new FakeCloudFileService { IsCloudOnly = true, HydrateResult = false };
        var service = new PhotoOrganizerService(fakeCloudService);

        var sampleFile = Path.Combine(_sourceDir, "IMG_20230308_143000.jpg");
        await File.WriteAllBytesAsync(sampleFile, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var progress = new Progress<OrganizeProgress>(_ => { });

        // Act
        var result = await service.OrganizeAsync(_sourceDir, _destDir, progress, CancellationToken.None, CloudFileHandlingMode.HydrateAndDehydrate);

        // Assert
        Assert.Equal(1, result.TotalScanned);
        Assert.Equal(0, result.CopiedCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Equal(1, fakeCloudService.HydrateCallCount);
        Assert.Equal(0, fakeCloudService.DehydrateCallCount);
    }

    [Fact]
    public void AppSettings_CloudFileMode_BackwardCompatibility_Works()
    {
        var settings = new AppSettings();

        // Default
        Assert.Equal(CloudFileHandlingMode.HydrateAndDehydrate, settings.CloudFileMode);
        Assert.False(settings.SkipCloudOnlyFiles);

        // Setting SkipCloudOnlyFiles = true should update CloudFileMode to Skip
        settings.SkipCloudOnlyFiles = true;
        Assert.Equal(CloudFileHandlingMode.Skip, settings.CloudFileMode);

        // Setting SkipCloudOnlyFiles = false should update CloudFileMode to HydrateAndDehydrate
        settings.SkipCloudOnlyFiles = false;
        Assert.Equal(CloudFileHandlingMode.HydrateAndDehydrate, settings.CloudFileMode);

        // Setting CloudFileMode to DownloadAndKeep
        settings.CloudFileMode = CloudFileHandlingMode.DownloadAndKeep;
        Assert.False(settings.SkipCloudOnlyFiles);
    }
}
