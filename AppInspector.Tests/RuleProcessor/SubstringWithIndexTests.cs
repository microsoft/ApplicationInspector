using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.RuleProcessor;

[TestClass]
[ExcludeFromCodeCoverage]
public class SubstringWithIndexTests
{
    private const string wordBoundaryDisabledCaseSensitive = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.WordBoundary"",
        ""tags"": [
            ""Testing.Rules.WordBoundary""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule finds race car with a word boundary."",
        ""patterns"": [
            {
                ""pattern"": ""racecar"",
                ""type"": ""substring"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    }
]";

    private const string wordBoundaryDisabledCaseInsensitive = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.WordBoundary"",
        ""tags"": [
            ""Testing.Rules.WordBoundary""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule finds race car with a word boundary."",
        ""patterns"": [
            {
                ""pattern"": ""racecar"",
                ""type"": ""substring"",
                ""confidence"": ""High"",
                ""modifiers"": [
                    ""i""
                ],
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    }
]";

    private const string wordBoundaryEnabledCaseSensitive = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.WordBoundary"",
        ""tags"": [
            ""Testing.Rules.WordBoundary""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule finds race car with a word boundary."",
        ""patterns"": [
            {
                ""pattern"": ""racecar"",
                ""type"": ""string"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    }
]";

    private const string wordBoundaryEnabledCaseInsensitive = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.WordBoundary"",
        ""tags"": [
            ""Testing.Rules.WordBoundary""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule finds race car with a word boundary."",
        ""patterns"": [
            {
                ""pattern"": ""racecar"",
                ""type"": ""string"",
                ""confidence"": ""High"",
                ""modifiers"": [
                    ""i""
                ],
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    }
]";

    private const string data = @"
raceCARwithmorestuff
racecarwithmorestuff
raceCAR withmorestuff
racecar withmorestuff";

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    [TestMethod]
    public void NoDictDataAllowed()
    {
        RuleSet rules = new();
        rules.AddString(wordBoundaryEnabledCaseSensitive, "TestRules");
        var theRule = rules.GetOatRules().First();
        theRule.Clauses.First().DictData = new List<KeyValuePair<string, string>>
            { new KeyValuePair<string, string>("test", "test") };

        Analyzer analyzer = new ApplicationInspectorAnalyzer();
        var issues = analyzer.EnumerateRuleIssues(theRule);

        Assert.AreEqual(1, issues.Count());
    }

    [TestMethod]
    public void NoData()
    {
        RuleSet rules = new();
        rules.AddString(wordBoundaryEnabledCaseSensitive, "TestRules");
        var theRule = rules.GetOatRules().First();
        theRule.Clauses.First().Data = new List<string>();

        Analyzer analyzer = new ApplicationInspectorAnalyzer();
        var issues = analyzer.EnumerateRuleIssues(theRule);

        Assert.AreEqual(1, issues.Count());
    }

    [TestMethod]
    public void WordBoundaryEnabledCaseSensitive()
    {
        RuleSet rules = new();
        rules.AddString(wordBoundaryEnabledCaseSensitive, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
        if (_languages.FromFileNameOut("test.c", out var info))
        {
            var matches = processor.AnalyzeFile(data, new FileEntry("test.cs", new MemoryStream()), info);
            Assert.AreEqual(1, matches.Count);
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void WordBoundaryDisabledCaseSensitive()
    {
        RuleSet rules = new();
        rules.AddString(wordBoundaryDisabledCaseSensitive, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
        if (_languages.FromFileNameOut("test.c", out var info))
        {
            var matches = processor.AnalyzeFile(data, new FileEntry("test.cs", new MemoryStream()), info);
            Assert.AreEqual(2, matches.Count);
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void WordBoundaryEnabledCaseInsensitive()
    {
        RuleSet rules = new();
        rules.AddString(wordBoundaryEnabledCaseInsensitive, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
        if (_languages.FromFileNameOut("test.c", out var info))
        {
            var matches = processor.AnalyzeFile(data, new FileEntry("test.cs", new MemoryStream()), info);
            Assert.AreEqual(2, matches.Count);
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void WordBoundaryDisabledCaseInsensitive()
    {
        RuleSet rules = new();
        rules.AddString(wordBoundaryDisabledCaseInsensitive, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
        if (_languages.FromFileNameOut("test.c", out var info))
        {
            var matches = processor.AnalyzeFile(data, new FileEntry("test.cs", new MemoryStream()), info);
            Assert.AreEqual(4, matches.Count);
        }
        else
        {
            Assert.Fail();
        }
    }
}