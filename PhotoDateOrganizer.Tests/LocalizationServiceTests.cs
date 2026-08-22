using System.Globalization;
using PhotoDateOrganizer.Services;
using Xunit;

namespace PhotoDateOrganizer.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void JapaneseStrings_ReturnsExpectedJapaneseText()
    {
        var service = new LocalizationService();
        service.SetCulture(new CultureInfo("ja-JP"));

        Assert.True(service.IsJapanese);
        Assert.IsType<JapaneseStrings>(service.CurrentStrings);
        Assert.Equal("フォルダ設定", service.CurrentStrings.FolderSetupTitle);
        Assert.Equal("整理を開始する", service.CurrentStrings.StartOrganizingButton);
        Assert.Equal("キャンセル", service.CurrentStrings.CancelButton);
        Assert.Contains("準備完了", service.CurrentStrings.StatusReady);
        Assert.Contains("免責事項", service.CurrentStrings.DisclaimerDialogTitle);
        Assert.Equal("同意して利用を開始する", service.CurrentStrings.DisclaimerAcceptButton);
        Assert.Equal("総検出数", service.CurrentStrings.StatTotal);
    }

    [Fact]
    public void EnglishStrings_ReturnsExpectedEnglishText()
    {
        var service = new LocalizationService();
        service.SetCulture(new CultureInfo("en-US"));

        Assert.False(service.IsJapanese);
        Assert.IsType<EnglishStrings>(service.CurrentStrings);
        Assert.Equal("Folder Setup", service.CurrentStrings.FolderSetupTitle);
        Assert.Equal("Start Organizing", service.CurrentStrings.StartOrganizingButton);
        Assert.Equal("Cancel", service.CurrentStrings.CancelButton);
        Assert.Contains("Ready", service.CurrentStrings.StatusReady);
        Assert.Contains("Disclaimer", service.CurrentStrings.DisclaimerDialogTitle);
        Assert.Equal("Accept and Start", service.CurrentStrings.DisclaimerAcceptButton);
        Assert.Equal("Total Found", service.CurrentStrings.StatTotal);
    }

    [Fact]
    public void AllDisclaimerItems_AreNonEmpty_InBothLanguages()
    {
        var ja = new JapaneseStrings();
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem1Title));
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem1Body));
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem2Title));
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem2Body));
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem3Title));
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem3Body));
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem4Title));
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem4Body));
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem5Title));
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem5Body));
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem6Title));
        Assert.False(string.IsNullOrWhiteSpace(ja.DisclaimerItem6Body));

        var en = new EnglishStrings();
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem1Title));
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem1Body));
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem2Title));
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem2Body));
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem3Title));
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem3Body));
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem4Title));
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem4Body));
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem5Title));
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem5Body));
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem6Title));
        Assert.False(string.IsNullOrWhiteSpace(en.DisclaimerItem6Body));
    }
}
