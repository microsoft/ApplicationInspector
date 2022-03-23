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
    public class RegexWithIndexTests
    {
        private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

        
        [TestMethod]
        public void NoDictDataAllowed()
        {
            RuleSet rules = new(null);
            rules.AddString(multiLineRule, "TestRules");
            var theRule = rules.GetOatRules().First();
            theRule.Clauses.First().DictData = new() { new KeyValuePair<string, string>("test","test") };

            Analyzer analyzer = new ApplicationInspectorAnalyzer();
            var issues = analyzer.EnumerateRuleIssues(theRule);

            Assert.AreEqual(1, issues.Count());
        }

        [TestMethod]
        public void NoData()
        {
            RuleSet rules = new(null);
            rules.AddString(multiLineRule, "TestRules");
            var theRule = rules.GetOatRules().First();
            theRule.Clauses.First().Data = new();

            Analyzer analyzer = new ApplicationInspectorAnalyzer();
            var issues = analyzer.EnumerateRuleIssues(theRule);

            Assert.AreEqual(1, issues.Count());
        }

        [TestMethod]
        public void InvalidRegex()
        {
            RuleSet rules = new(null);
            rules.AddString(multiLineRule, "TestRules");
            var theRule = rules.GetOatRules().First();
            theRule.Clauses.First().Data = new() { "^($" };

            Analyzer analyzer = new ApplicationInspectorAnalyzer();
            var issues = analyzer.EnumerateRuleIssues(theRule);

            Assert.AreEqual(1, issues.Count());
        }
        
        [TestMethod]
        public void InvalidRegexWhenAnalyzing()
        {
            RuleSet rules = new(null);
            rules.AddString(multiLineRule, "TestRules");
            var theRule = rules.GetOatRules().First();
            theRule.Clauses.First().Data = new() { "^($" };

            Analyzer analyzer = new ApplicationInspectorAnalyzer();
            var issues = analyzer.Analyze(rules.GetOatRules(),
                new TextContainer("TestContent", "csharp", new Microsoft.ApplicationInspector.RulesEngine.Languages()));

            Assert.AreEqual(0, issues.Count());
        }

        [TestMethod]
        public void MultiLine()
        {
            RuleSet rules = new(null);
            rules.AddString(multiLineRule, "TestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("test.c", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(multiLineData, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
                Assert.AreEqual(1, matches.Count);
            }
            else
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void MultiLineCaseInsensitive()
        {
            RuleSet rules = new(null);
            rules.AddString(multiLineCaseInsensitiveRule, "TestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("test.c", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(multiLineData, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
                Assert.AreEqual(2, matches.Count);
            }
            else
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void MultiLineRuleWithSingleLineData()
        {
            RuleSet rules = new(null);
            rules.AddString(multiLineRule, "TestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("test.c", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(singleLineData, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
                Assert.AreEqual(0, matches.Count);
            }
            else
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void MultiLineRuleWithoutOptionSet()
        {
            RuleSet rules = new(null);
            rules.AddString(multiLineRuleWithoutMultiLine, "TestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("test.c", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(singleLineData, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
                Assert.AreEqual(0, matches.Count);
            }
            else
            {
                Assert.Fail();
            }
        }

        private const string multiLineRuleWithoutMultiLine = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.MultiLine"",
        ""tags"": [
            ""Testing.Rules.MultiLine""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule finds race car split across two lines."",
        ""patterns"": [
            {
                ""pattern"": ""race\\r\\ncar"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    }
]";

        private const string multiLineRule = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.MultiLine"",
        ""tags"": [
            ""Testing.Rules.MultiLine""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule finds race car split across two lines."",
        ""patterns"": [
            {
                ""pattern"": ""race\\r\\ncar"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""modifiers"": [
                    ""m""
                ],
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    }
]";

        private const string multiLineCaseInsensitiveRule = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.MultiLine"",
        ""tags"": [
            ""Testing.Rules.MultiLine""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule finds race car split across two lines."",
        ""patterns"": [
            {
                ""pattern"": ""race\\r\\ncar"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""modifiers"": [
                    ""m"",
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

        const string multiLineData = @"
race
CAR
race
car";

        const string singleLineData = @"
raceCAR
racecar";
    }
}