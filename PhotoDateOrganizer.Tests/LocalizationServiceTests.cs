using System;
using System.Globalization;
using PhotoDateOrganizer.Services;
using Xunit;

namespace PhotoDateOrganizer.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void ResolveStrings_ExplicitJapanese_ReturnsJapaneseStrings()
    {
        var strings = LocalizationService.ResolveStrings(AppLanguage.Japanese);
        Assert.IsType<JapaneseStrings>(strings);
        Assert.Contains("写真", strings.AppTitle);
    }

    [Fact]
    public void ResolveStrings_ExplicitEnglish_ReturnsEnglishStrings()
    {
        var strings = LocalizationService.ResolveStrings(AppLanguage.English);
        Assert.IsType<EnglishStrings>(strings);
        Assert.Contains("Organize", strings.AppTitle);
    }

    [Fact]
    public void ResolveStrings_AutoWithJapaneseCulture_ReturnsJapaneseStrings()
    {
        var jaCulture = new CultureInfo("ja-JP");
        var strings = LocalizationService.ResolveStrings(AppLanguage.Auto, jaCulture);
        Assert.IsType<JapaneseStrings>(strings);
        Assert.Equal("整理を開始する", strings.StartButton);
    }

    [Fact]
    public void ResolveStrings_AutoWithEnglishCulture_ReturnsEnglishStrings()
    {
        var enCulture = new CultureInfo("en-US");
        var strings = LocalizationService.ResolveStrings(AppLanguage.Auto, enCulture);
        Assert.IsType<EnglishStrings>(strings);
        Assert.Equal("Start Organizing", strings.StartButton);
    }

    [Fact]
    public void ResolveStrings_AutoWithOtherCulture_FallsBackToEnglishStrings()
    {
        var frCulture = new CultureInfo("fr-FR");
        var strings = LocalizationService.ResolveStrings(AppLanguage.Auto, frCulture);
        Assert.IsType<EnglishStrings>(strings);
        Assert.Equal("Start Organizing", strings.StartButton);
    }

    [Fact]
    public void JapaneseStrings_FormattedMethods_ReturnValidStrings()
    {
        var ja = new JapaneseStrings();
        var dt = new DateTime(2023, 5, 12, 14, 30, 0);

        Assert.Contains("C:\\Test", ja.SourceFolderSetFormat("C:\\Test"));
        Assert.Contains("D:\\Output", ja.DestinationFolderSetFormat("D:\\Output"));
        Assert.Contains("5", ja.FallbackNoticeFormat(5));
        Assert.Contains("10", ja.OrganizeCompleteWithFallbackFormat(10, 2, 1, TimeSpan.FromSeconds(5)));
        Assert.Contains("2023-05-12", ja.NoteExif(dt));
        Assert.Contains("2023-05-12", ja.NoteQuickTime(dt));
        Assert.Contains("2023-05-12", ja.NoteFilenamePattern(dt));
        Assert.NotEmpty(ja.DisclaimerDialogTitle);
        Assert.NotEmpty(ja.DisclaimerItem1Title);
        Assert.NotEmpty(ja.DisclaimerItem1Desc);
        Assert.NotEmpty(ja.DisclaimerItem6Title);
        Assert.NotEmpty(ja.DisclaimerItem6Desc);
    }

    [Fact]
    public void EnglishStrings_FormattedMethods_ReturnValidStrings()
    {
        var en = new EnglishStrings();
        var dt = new DateTime(2023, 5, 12, 14, 30, 0);

        Assert.Contains("C:\\Test", en.SourceFolderSetFormat("C:\\Test"));
        Assert.Contains("D:\\Output", en.DestinationFolderSetFormat("D:\\Output"));
        Assert.Contains("5", en.FallbackNoticeFormat(5));
        Assert.Contains("10", en.OrganizeCompleteWithFallbackFormat(10, 2, 1, TimeSpan.FromSeconds(5)));
        Assert.Contains("2023-05-12", en.NoteExif(dt));
        Assert.Contains("2023-05-12", en.NoteQuickTime(dt));
        Assert.Contains("2023-05-12", en.NoteFilenamePattern(dt));
        Assert.NotEmpty(en.DisclaimerDialogTitle);
        Assert.NotEmpty(en.DisclaimerItem1Title);
        Assert.NotEmpty(en.DisclaimerItem1Desc);
        Assert.NotEmpty(en.DisclaimerItem6Title);
        Assert.NotEmpty(en.DisclaimerItem6Desc);
    }

    [Fact]
    public void LocalizationService_LanguageSwitch_FiresPropertyChanged()
    {
        var service = LocalizationService.Current;
        bool propertyChangedFired = false;
        string? changedPropertyName = null;

        void Handler(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            propertyChangedFired = true;
            changedPropertyName = e.PropertyName;
        }

        service.PropertyChanged += Handler;

        try
        {
            service.Language = AppLanguage.English;
            Assert.True(propertyChangedFired);
            Assert.Equal(AppLanguage.English, service.Language);
            Assert.True(service.IsEnglish);
            Assert.False(service.IsJapanese);

            propertyChangedFired = false;
            service.Language = AppLanguage.Japanese;
            Assert.True(propertyChangedFired);
            Assert.Equal(AppLanguage.Japanese, service.Language);
            Assert.True(service.IsJapanese);
            Assert.False(service.IsEnglish);
        }
        finally
        {
            service.PropertyChanged -= Handler;
            service.Language = AppLanguage.Auto;
        }
    }
}
