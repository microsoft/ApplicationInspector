using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.RuleProcessor
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class SubstringWithIndexTests
    {
        private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

        [TestMethod]
        public void NoDictDataAllowed()
        {
            RuleSet rules = new(null);
            rules.AddString(wordBoundaryEnabledCaseSensitive, "TestRules");
            var theRule = rules.GetOatRules().First();
            theRule.Clauses.First().DictData = new() { new KeyValuePair<string, string>("test", "test") };

            Analyzer analyzer = new ApplicationInspectorAnalyzer();
            var issues = analyzer.EnumerateRuleIssues(theRule);

            Assert.AreEqual(1, issues.Count());
        }

        [TestMethod]
        public void NoData()
        {
            RuleSet rules = new(null);
            rules.AddString(wordBoundaryEnabledCaseSensitive, "TestRules");
            var theRule = rules.GetOatRules().First();
            theRule.Clauses.First().Data = new();

            Analyzer analyzer = new ApplicationInspectorAnalyzer();
            var issues = analyzer.EnumerateRuleIssues(theRule);

            Assert.AreEqual(1, issues.Count());
        }

        [TestMethod]
        public void WordBoundaryEnabledCaseSensitive()
        {
            RuleSet rules = new(null);
            rules.AddString(wordBoundaryEnabledCaseSensitive, "TestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("test.c", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(data, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
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
            RuleSet rules = new(null);
            rules.AddString(wordBoundaryDisabledCaseSensitive, "TestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("test.c", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(data, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
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
            RuleSet rules = new(null);
            rules.AddString(wordBoundaryEnabledCaseInsensitive, "TestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("test.c", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(data, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
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
            RuleSet rules = new(null);
            rules.AddString(wordBoundaryDisabledCaseInsensitive, "TestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("test.c", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(data, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
                Assert.AreEqual(4, matches.Count);
            }
            else
            {
                Assert.Fail();
            }
        }

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

        const string data = @"
raceCARwithmorestuff
racecarwithmorestuff
raceCAR withmorestuff
racecar withmorestuff";
    }
}