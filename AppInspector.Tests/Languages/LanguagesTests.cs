using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Events;
using Xunit;
using Assert = Xunit.Assert;

namespace AppInspector.Tests.Languages;

[ExcludeFromCodeCoverage]
public class LanguagesTests
{
    private static ILoggerFactory _factory = new NullLoggerFactory();
    private static string invalidTestCommentsPath = string.Empty;
    private static string invalidTestLanguagesPath = string.Empty;
    private static string testCommentsPath = string.Empty;

    private static string testLanguagesPath = string.Empty;

    public LanguagesTests()
    {
        testLanguagesPath = Path.Combine("TestData","TestLanguages", "test_languages.json");
        testCommentsPath = Path.Combine("TestData","TestLanguages", "test_comments.json");
        invalidTestLanguagesPath = Path.Combine("TestData","TestLanguages", 
            "test_languages_invalid.json");
        invalidTestCommentsPath =
            Path.Combine("TestData","TestLanguages", "test_comments_invalid.json");
        _factory = new LogOptions { ConsoleVerbosityLevel = LogEventLevel.Verbose }.GetLoggerFactory();
    }

    [Fact]
    public void DetectCustomLanguage()
    {
        var languages =
            Microsoft.ApplicationInspector.RulesEngine.Languages.FromConfigurationFiles(_factory, testCommentsPath,
                testLanguagesPath);
        Assert.True(languages.FromFileNameOut("afilename.z", out var language));
        Assert.Equal("z", language.Name);
        Assert.False(languages.FromFileNameOut("afilename.c", out var _));
    }

    [Fact]
    public void EmptyLanguagesOnInvalidCommentsAndLanguages()
    {
        Assert.Throws<JsonException>(() =>
            Microsoft.ApplicationInspector.RulesEngine.Languages.FromConfigurationFiles(_factory,
                invalidTestCommentsPath));
        Assert.Throws<JsonException>(() =>
            Microsoft.ApplicationInspector.RulesEngine.Languages.FromConfigurationFiles(_factory, null,
                invalidTestLanguagesPath));
    }

    [Fact]
    public void DetectLanguageAsFileNameLanguage()
    {
        Microsoft.ApplicationInspector.RulesEngine.Languages languages = new(_factory);
        Assert.True(languages.FromFileNameOut("package.json", out var language));
        Assert.Equal("package.json", language.Name);
    }

    [InlineData(null, false)] // No way to determine language
    [InlineData("", false)] // No way to determine language
    [InlineData("validfilename.json", false)] //This test uses the .z test comments and languages from this file.
    [InlineData("validfilename.z", true)]
    [Theory]
    public void ReturnFalseWithInvalidFilename(string? filename, bool expected)
    {
        var languages =
            Microsoft.ApplicationInspector.RulesEngine.Languages.FromConfigurationFiles(_factory, testCommentsPath,
                testLanguagesPath);
        Assert.Equal(expected, languages.FromFileNameOut(filename!, out _));
    }
    
        
    [Fact]
    public void ScopeMatchAlwaysCommented()
    {
        var _languages = new Microsoft.ApplicationInspector.RulesEngine.Languages();
        var textContainer = new TextContainer("Hello this is some content", "plaintext", _languages);
        var boundary = new Boundary() { Index = 1, Length = 2 };
        Assert.True(textContainer.ScopeMatch(new List<PatternScope>() { PatternScope.All }, boundary));
        Assert.False(textContainer.ScopeMatch(new List<PatternScope>() { PatternScope.Code }, boundary));
        Assert.True(textContainer.ScopeMatch(new List<PatternScope>() { PatternScope.Comment }, boundary));
    }
}