using System;
using System.IO;
using PhotoDateOrganizer.Models;
using PhotoDateOrganizer.Services;
using Xunit;

namespace PhotoDateOrganizer.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public SettingsServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "PhotoDateOrganizer_SettingsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void ExportAndImportSettings_ShouldPreserveValues()
    {
        // Arrange
        var service = new SettingsService();
        var filePath = Path.Combine(_tempDirectory, "exported_settings.json");
        var originalSettings = new AppSettings
        {
            SourceDirectory = @"C:\Photos\Input",
            DestinationDirectory = @"D:\Photos\Organized",
            WindowX = 150,
            WindowY = 200,
            WindowWidth = 1280,
            WindowHeight = 800
        };

        // Act
        service.ExportSettings(filePath, originalSettings);
        var importedSettings = service.ImportSettings(filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        Assert.Equal(originalSettings.SourceDirectory, importedSettings.SourceDirectory);
        Assert.Equal(originalSettings.DestinationDirectory, importedSettings.DestinationDirectory);
        Assert.Equal(originalSettings.WindowX, importedSettings.WindowX);
        Assert.Equal(originalSettings.WindowY, importedSettings.WindowY);
        Assert.Equal(originalSettings.WindowWidth, importedSettings.WindowWidth);
        Assert.Equal(originalSettings.WindowHeight, importedSettings.WindowHeight);
    }

    [Fact]
    public void ImportSettings_FileNotFound_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var service = new SettingsService();
        var nonExistentPath = Path.Combine(_tempDirectory, "non_existent.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => service.ImportSettings(nonExistentPath));
    }

    [Fact]
    public void ExportSettings_CreatesTargetDirectoryIfNotExists()
    {
        // Arrange
        var service = new SettingsService();
        var nestedPath = Path.Combine(_tempDirectory, "sub1", "sub2", "settings.json");
        var settings = new AppSettings
        {
            SourceDirectory = @"C:\Test"
        };

        // Act
        service.ExportSettings(nestedPath, settings);

        // Assert
        Assert.True(File.Exists(nestedPath));
        var imported = service.ImportSettings(nestedPath);
        Assert.Equal(@"C:\Test", imported.SourceDirectory);
    }
}
