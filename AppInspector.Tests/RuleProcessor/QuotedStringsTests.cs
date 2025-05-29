using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
/// Tests for properly detecting commented/live code status in the presence of comment markers inside of quoted strings
/// </summary>
[TestClass]
public class QuotedStringsTests
{

    private const string testDoubleQuotesAreCode = "var url = \"https://contoso.com\"; // contoso.com";
    private const string testSingleQuotesAreCode = "var url = 'https://contoso.com'; // contoso.com";
    private const string testSingleLineWithQuotesInComment = "// var url = 'https://contoso.com';";
    private const string testSingleLineWithDoubleQuotesInComment = "// var url = 'https://contoso.com';";
    private const string testMultiLine = @"/* 
https://contoso.com 
*/";
    private const string testMultiLineWithoutProto = @"
/* 
contoso.com 
*/";
    private const string testMultiLineWithResultFollowingCommentEnd = @"
/* 
contoso.com 
*/ var url = ""https://contoso.com""";
    private const string testRubyInterpolatedStrings = @"findMe = ""findMe""
puts ""Hello, #{findMe}!"" # findMe
def inspect # :nodoc:
    ""#<#{findMe} #{findMe}>"" #findMe
end
"; // Should find 5 instances, excluding the two true comments

    private static string detectContosoRule = @"
    [
    {
        ""id"": ""RE000001"",
        ""name"": ""Testing.Rules.Quotes"",
        ""tags"": [
            ""Testing.Rules.Quotes""
        ],
        ""severity"": ""Critical"",
        ""description"": ""Find contoso.com"",
        ""patterns"": [
            {
                ""pattern"": ""contoso.com"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    }
]
";

    private static string detectFindMeRule = @"
    [
    {
        ""id"": ""RE000001"",
        ""name"": ""Testing.Rules.Quotes"",
        ""tags"": [
            ""Testing.Rules.Quotes""
        ],
        ""severity"": ""Critical"",
        ""description"": ""Find findMe"",
        ""patterns"": [
            {
                ""pattern"": ""findMe"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    }
]
";

    private readonly ILoggerFactory _loggerFactory =
        new LogOptions { ConsoleVerbosityLevel = LogEventLevel.Verbose }.GetLoggerFactory();

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();
    
    [DataRow(testDoubleQuotesAreCode,1)]
    [DataRow(testSingleQuotesAreCode,1)]
    [DataRow(testMultiLine,0)]
    [DataRow(testMultiLineWithoutProto,0)]
    [DataRow(testMultiLineWithResultFollowingCommentEnd,1)]
    [DataRow(testSingleLineWithQuotesInComment,0)]
    [DataRow(testSingleLineWithDoubleQuotesInComment,0)]
    [TestMethod]
    public void QuotedStrings(string content, int numIssues)
    {
        RuleSet rules = new(_loggerFactory);
        rules.AddString(detectContosoRule, "contosorule");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor ruleProcessor =
            new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
        _languages.FromFileNameOut("testfile.cs", out LanguageInfo info);
        Assert.AreEqual(numIssues,
            ruleProcessor.AnalyzeFile(content, new FileEntry("testfile.cs", new MemoryStream()), info).Count());
    }

    /// <summary>
    /// Ruby interpolated strings provide an interesting test case because they use the comment character as part of interpolation
    ///     the comment marker is one character long, and it may often come right after the quotation mark
    /// </summary>
    /// <param name="content"></param>
    /// <param name="numIssues"></param>
    
    [DataRow(testRubyInterpolatedStrings, 5)]
    [TestMethod]
    public void QuotedStringsRuby(string content, int numIssues)
    {
        RuleSet rules = new(_loggerFactory);
        rules.AddString(detectFindMeRule, "findMeRule");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor ruleProcessor =
            new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
        _languages.FromFileNameOut("testfile.rb", out LanguageInfo info);
        Assert.AreEqual(numIssues,
            ruleProcessor.AnalyzeFile(content, new FileEntry("testfile.rb", new MemoryStream()), info).Count());
    }
}