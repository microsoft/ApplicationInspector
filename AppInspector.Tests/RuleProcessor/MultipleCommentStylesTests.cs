using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     Tests for languages which support more than one style of comment, for example Razor pages which support both
///     html and C# style comments as well as razor specific comments.
/// </summary>
public class MultipleCommentStylesTests
{
    private const string detectContosoRule = @"
    [
    {
        ""id"": ""RE000001"",
        ""name"": ""Testing.Rules.MultipleComments"",
        ""tags"": [
            ""Testing.Rules.MultipleComments""
        ],
        ""severity"": ""Critical"",
        ""description"": ""Find contoso.com"",
        ""patterns"": [
            {
                ""pattern"": ""contoso.com"",
                ""type"": ""string"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ]
            }
        ]
    }
]
";

    private readonly ILoggerFactory _loggerFactory =
        new LogOptions { ConsoleVerbosityLevel = LogEventLevel.Verbose }.GetLoggerFactory();

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    private int NumCodeMatches(string content, string fileName)
    {
        RuleSet rules = new(_loggerFactory);
        rules.AddString(detectContosoRule, "contosorule");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor ruleProcessor = new(rules, new RuleProcessorOptions());
        _languages.FromFileNameOut(fileName, out LanguageInfo info);
        return ruleProcessor.AnalyzeFile(content, new FileEntry(fileName, new MemoryStream()), info).Count();
    }

    [InlineData("@* contoso.com *@", 0)] // Razor comment
    [InlineData("<!-- contoso.com -->", 0)] // Html comment
    [InlineData("@{ /* contoso.com */ }", 0)] // C# block comment
    [InlineData("@{ // contoso.com\n }", 0)] // C# inline comment
    [InlineData("@* A razor comment isn't ended by --> *@ contoso.com", 1)]
    [InlineData("<!-- An html comment isn't ended by *@ --> contoso.com", 1)]
    [InlineData("@* <!-- contoso.com --> *@", 0)] // Nested marker of another style is still commented
    [InlineData("<a href=\"https://contoso.com\">Link</a>", 1)]
    [InlineData("@* An unterminated comment runs to the end of the file contoso.com", 0)]
    [Theory]
    public void RazorSupportsMultipleCommentStyles(string content, int numIssues)
    {
        Assert.Equal(numIssues, NumCodeMatches(content, "testfile.razor"));
    }

    [InlineData("# contoso.com", 0)]
    [InlineData("// contoso.com", 0)]
    [InlineData("/* contoso.com */", 0)]
    [InlineData("url = \"https://contoso.com\"", 1)]
    [InlineData("url = \"https://contoso.com\" # A comment", 1)]
    [InlineData("url = \"https://contoso.com\" // A comment", 1)]
    [Theory]
    public void TerraformSupportsMultipleInlineCommentStyles(string content, int numIssues)
    {
        Assert.Equal(numIssues, NumCodeMatches(content, "testfile.tf"));
    }

    [Fact]
    public void LanguagesReturnsAllCommentStyles()
    {
        Assert.Equal(new[] { "//" }, _languages.GetCommentInlines("razor"));
        Assert.Equal(new[] { ("@*", "*@"), ("<!--", "-->"), ("/*", "*/") },
            _languages.GetCommentBlocks("razor"));
        Assert.Equal(new[] { "#", "//" }, _languages.GetCommentInlines("terraform"));
        Assert.Equal(new[] { ("/*", "*/") }, _languages.GetCommentBlocks("terraform"));
    }

    [Fact]
    public void LanguagesSingleCommentStyleAccessorsReturnFirstStyle()
    {
        Assert.Equal("//", _languages.GetCommentInline("razor"));
        Assert.Equal("@*", _languages.GetCommentPrefix("razor"));
        Assert.Equal("*@", _languages.GetCommentSuffix("razor"));
        Assert.Equal(string.Empty, _languages.GetCommentPrefix("python"));
        Assert.Equal(string.Empty, _languages.GetCommentSuffix("python"));
    }

    [Fact]
    public void BlockCommentIsOnlyEndedByItsOwnSuffix()
    {
        var textContainer = new TextContainer("@* <!-- *@X", "razor", _languages);
        // The razor comment, including the closing marker, is commented
        for (var i = 0; i < 10; i++) Assert.True(textContainer.IsCommented(i));
        // The character after the closing marker is code
        Assert.False(textContainer.IsCommented(10));
    }
}
