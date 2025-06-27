using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.CST.RecursiveExtractor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace AppInspector.Tests.RuleProcessor
{
        public class RegexModifiersTests
    {
        private const string regexWithoutLookBehind = @"[
    {{
        ""id"": ""RE000001"",
        ""name"": ""Testing.Rules.Regex"",
        ""tags"": [
            ""Testing.Rules.Regex""
        ],
        ""severity"": ""Critical"",
        ""description"": ""Regex without lookbehind"",
        ""patterns"": [
            {{
                ""pattern"": ""^[0-9A-Z]([-.\\w]*[0-9A-Z])?@"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""modifiers"": [
                    {0}
                ],
                ""scopes"": [
                    ""code""
                ]
            }}
        ],
        ""_comment"": """"
    }}
]";

        private const string regexWithLookBehind = @"[
    {{
        ""id"": ""RE000001"",
        ""name"": ""Testing.Rules.Regex"",
        ""tags"": [
            ""Testing.Rules.Regex""
        ],
        ""severity"": ""Critical"",
        ""description"": ""Regex with lookbehind"",
        ""patterns"": [
            {{
                ""pattern"": ""^[0-9A-Z][-.\\w]*(?<=[0-9A-Z])@"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""modifiers"": [
                    {0}
                ],
                ""scopes"": [
                    ""code""
                ]
            }}
        ],
        ""_comment"": """"
    }}
]";
        private const string modifier_i = "\"i\"";
        private const string modifier_ib = "\"i\", \"b\"";
        private const string modifier_iB = "\"i\", \"B\"";
        private const string modifier_inb = "\"i\", \"nb\"";
        private const string modifier_iNB = "\"i\", \"NB\"";
        private const string modifier_ibnb = "\"i\", \"b\", \"nb\"";
        private const string modifier_iBNB = "\"i\", \"B\", \"NB\"";

        private const string actualModifer_inb = "i,nb";
        private const string actualModifer_ib = "b,i";
        private const string actualModifer_iNB = "i,NB";
        private const string actualModifer_iB = "B,i";
        private const string actualModifer_i = "i";

        private const string input = "application@inspector.com";

        private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

        private static List<MatchRecord> GetMatches(Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor, LanguageInfo info)
        {
            return processor.AnalyzeFile(input, new FileEntry("test.cs", new MemoryStream()), info);
        }

        [InlineData(false, modifier_i)]
        [InlineData(true, modifier_i)]
        [Theory]
        public void InvalidRegex(bool enableNonBacktracking, string modifiers)
        {
            RuleSet rules = new() {  EnableNonBacktrackingRegex = enableNonBacktracking };
            rules.AddString(string.Format(regexWithoutLookBehind, modifiers), "TestRules");
            var theRule = rules.GetOatRules().First();
            theRule.Clauses.First().Data = new List<string> { "^($" };

            Analyzer analyzer = new ApplicationInspectorAnalyzer();
            var issues = analyzer.EnumerateRuleIssues(theRule);

            Assert.Single(issues);
            Assert.Equal("Err_ClauseInvalidRegex", issues.First().Description);
        }

        [InlineData(false, modifier_i, actualModifer_i)]        // nb not added, b not added
        [InlineData(false, modifier_ib, actualModifer_ib)]      // nb not added, b added
        [InlineData(false, modifier_iB, actualModifer_iB)]      // nb not added, B added
        [InlineData(false, modifier_inb, actualModifer_inb)]    // nb present, nb added 
        [InlineData(false, modifier_iNB, actualModifer_iNB)]    // NB present, NB added
        [InlineData(false, modifier_ibnb, actualModifer_inb)]   // b and nb present, nb added
        [InlineData(false, modifier_iBNB, actualModifer_iNB)]   // B and NB present, NB added
        [InlineData(true, modifier_i, actualModifer_inb)]       // nb not present, nb added
        [InlineData(true, modifier_inb, actualModifer_inb)]     // nb present, nb added
        [InlineData(true, modifier_iNB, actualModifer_iNB)]     // NB present, NB added
        [InlineData(true, modifier_ib, actualModifer_ib)]       // b present, b added
        [InlineData(true, modifier_iB, actualModifer_iB)]       // B present, B added
        [InlineData(true, modifier_ibnb, actualModifer_ib)]     // b and nb present, b added
        [InlineData(true, modifier_iBNB, actualModifer_iB)]     // B and NB present, b added
        [Theory]
        public void ValidateModifiers(bool enableNonBacktracking, string inputModifiers, string expectedModifiers)
        {
            RuleSet rules = new() { EnableNonBacktrackingRegex = enableNonBacktracking };

            rules.AddString(string.Format(regexWithoutLookBehind, inputModifiers), "TestRules");
            Assert.Single(rules);

            var patterns = rules.First().Patterns;
            Assert.Single(patterns);

            var modifiers = patterns.First().Modifiers;            
            var actualModifiers = string.Join(",", modifiers.OrderBy(m => m));
            Assert.Equal(expectedModifiers, actualModifiers);
        }

        [Theory]
        [CombinatorialData]
        public void RegexWithLookBehind(bool enableNonBacktracking)
        {
            RuleSet rules = new() { EnableNonBacktrackingRegex = enableNonBacktracking };

            rules.AddString(string.Format(regexWithLookBehind, modifier_i), "TestRules");

            Assert.Single(rules);      

            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("te" +
                "st.c", out var info))
            {
                // not throwing exception here, the code will remove non-backtracking directive and produce a single match
                var matches = GetMatches(processor, info);
                Assert.Single(matches);
            }
            else
            {
                Assert.Fail();
            }
        }

        [Theory]
        [CombinatorialData]
        public void RegexWithoutLookBehind(bool enableNonBacktracking)
        {
            RuleSet rules = new() { EnableNonBacktrackingRegex = enableNonBacktracking };

            rules.AddString(string.Format(regexWithoutLookBehind, modifier_i), "TestRules");

            Assert.Single(rules);

            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("te" +
                "st.c", out var info))
            {
                // "no look behind" will work with both options (backtracking and non-backtracking)
                var matches = GetMatches(processor, info);
                Assert.Single(matches);
            }
            else
            {
                Assert.Fail();
            }

            static List<MatchRecord> GetMatches(Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor, LanguageInfo info)
            {
                return processor.AnalyzeFile(input, new FileEntry("test.cs", new MemoryStream()), info);
            }
        }
    }
}
