using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.CLI;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace AppInspector.Tests.RuleProcessor
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class WithinClauseTests
    {
        private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

        [DataRow("WithinClauseWithInvertWithFindingRange")]
        [DataRow("WithinClauseWithoutInvertWithFindingRange")]
        [DataRow("WithinClauseWithInvertWithSameLine")]
        [DataRow("WithinClauseWithoutInvertWithSameLine")]
        [DataTestMethod]
        public void WithinClauseInvertTest(string testDataKey)
        {
            WithinClauseInvertTest(testData[testDataKey].testData, testData[testDataKey].conditionRegion, testData[testDataKey].negate, testData[testDataKey].expectedNumMatches, testData[testDataKey].expectedMatchesLineStarts);
        }

        internal void WithinClauseInvertTest(string testData, string condition_region, bool invert, int expectedMatches, int[] expectedMatchesLineStarts)
        {
            RuleSet rules = new(_loggerFactory);
            var newRule = baseRule.Replace("REPLACE_REGION", condition_region).Replace("REPLACE_NEGATE", invert.ToString().ToLowerInvariant());
            rules.AddString(newRule, "TestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("test.c", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(testData, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
                Assert.AreEqual(expectedMatches, matches.Count);
                foreach (int expectedMatchLineStart in expectedMatchesLineStarts)
                {
                    MatchRecord? correctLineMatch = matches.FirstOrDefault(match => match.StartLocationLine == expectedMatchLineStart);
                    Assert.IsNotNull(correctLineMatch);
                    matches.Remove(correctLineMatch);
                }
            }
            else
            {
                Assert.Fail();
            }
        }

        [DataRow(true, 1, new int[] { 2 })]
        [DataRow(false, 1, new int[] { 3 })]
        [DataTestMethod]
        public void WithinClauseInvertTestForSameLine(bool invert, int expectedMatches, int[] expectedMatchesLineStarts)
        {
            RuleSet rules = new(_loggerFactory);
            var newRule = findingOnlyRule.Replace("REPLACE_NEGATE", invert.ToString().ToLowerInvariant());
            rules.AddString(newRule, "TestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("test.c", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(insideFindingData, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
                Assert.AreEqual(expectedMatches, matches.Count);
                foreach (int expectedMatchLineStart in expectedMatchesLineStarts)
                {
                    MatchRecord? correctLineMatch = matches.FirstOrDefault(match => match.StartLocationLine == expectedMatchLineStart);
                    Assert.IsNotNull(correctLineMatch);
                    matches.Remove(correctLineMatch);
                }
            }
            else
            {
                Assert.Fail();
            }
        }
        
        [DataRow(true, true, true, 0, 0, 1)]
        [DataRow(true, false, true, 0, 0, 1)]

        [DataRow(true, true, false, 0, 0, 1)]
        [DataRow(false, true, true, 0, 0, 1)]
        [DataRow(true, false, false, 0, 0, 0)]
        [DataRow(false, false, true, 0, 0, 0)]
        [DataRow(false, true, false, 0, 0, 1)]
        [DataRow(false, true, false, 0, 1, 1)]
        [DataRow(false, true, false, 1, -1, 0)]
        [DataRow(false, true, false, -1, 0, 1)]

        [DataTestMethod]
        public void WithinClauseValidationTest(bool findingOnlySetting, bool findingRegionSetting, bool sameLineOnlySetting, int afterSetting, int beforeSetting, int expectedNumIssues)
        {
            RuleSet rules = new(_loggerFactory);
            rules.AddString(validationRule, "TestRules");
            IEnumerable<WithinClause> withinClauses = rules
                .GetOatRules()
                .SelectMany(x => x.Clauses)
                .OfType<WithinClause>();
            foreach (WithinClause clause in withinClauses)
            {
                clause.FindingOnly = findingOnlySetting;
                clause.FindingRegion = findingRegionSetting;
                clause.SameLineOnly = sameLineOnlySetting;
                clause.After = afterSetting;
                clause.Before = beforeSetting;
            }
            RulesVerifier verifier = new(new RulesVerifierOptions() {LoggerFactory = _loggerFactory});
            var oatIssues = verifier.CheckIntegrity(rules).SelectMany(x => x.OatIssues);
            foreach (var violation in oatIssues)
            {
                _logger.LogDebug(violation.Description);
            }
            Assert.AreEqual(expectedNumIssues, verifier.CheckIntegrity(rules).Sum(x => x.OatIssues.Count()));
        }

        [DataRow(true, 1, new int[] { 2 })]
        [DataRow(false, 1, new int[] { 3 })]
        [DataTestMethod]
        public void WithinClauseInvertTestForFindingRange0(bool invert, int expectedMatches, int[] expectedMatchesLineStarts)
        {
            RuleSet rules = new(_loggerFactory);
            var newRule = findingRangeZeroRule.Replace("REPLACE_NEGATE", invert.ToString().ToLowerInvariant());
            rules.AddString(newRule, "TestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (_languages.FromFileNameOut("test.c", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(insideFindingData, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
                Assert.AreEqual(expectedMatches, matches.Count);
                foreach (int expectedMatchLineStart in expectedMatchesLineStarts)
                {
                    MatchRecord? correctLineMatch = matches.FirstOrDefault(match => match.StartLocationLine == expectedMatchLineStart);
                    Assert.IsNotNull(correctLineMatch);
                    matches.Remove(correctLineMatch);
                }
            }
            else
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void MultiLineRegexCondition()
        {
            RuleSet rules = new(_loggerFactory);
            rules.AddString(multiLineRule, "multiline-tests");
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

        Dictionary<string, (string testData, string conditionRegion, bool negate, int expectedNumMatches, int[] expectedMatchesLineStarts)> testData = new()
        {
            {
                "WithinClauseWithInvertWithFindingRange",
                (findingRangeData, "finding-region(0,1)", true, 1, new int[] { 13 })
            },
            {
                "WithinClauseWithoutInvertWithFindingRange",
                (findingRangeData, "finding-region(0,1)", false, 1, new int[] { 14 })
            },
            {
                "WithinClauseWithInvertWithSameLine",
                (sameLineData, "same-line", true, 1, new int[] { 13 })
            },
            {
                "WithinClauseWithoutInvertWithSameLine",
                (sameLineData, "same-line", false, 1, new int[] { 14 })
            }
        };

        [ClassInitialize]
        public void ClassInit()
        {
            _logger = _loggerFactory.CreateLogger<WithinClauseTests>();
        }
        
        private ILoggerFactory _loggerFactory = new LogOptions(){ ConsoleVerbosityLevel = LogEventLevel.Verbose }.GetLoggerFactory();
        private ILogger _logger;

        private const string validationRule = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.MallocNotFree1"",
        ""tags"": [
            ""Testing.Rules.MallocNotFree1""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule aims to find malloc() that does NOT have free() in 1 line range"",
        ""patterns"": [
            {
                ""pattern"": ""racecar"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""modifiers"": [
                    ""i""
                ],
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""conditions"": [
            {
                ""pattern"": {
                    ""pattern"": ""car"",
                    ""type"": ""regex"",
                    ""scopes"": [
                        ""code""
                    ]
                },
                ""search_in"": ""same-line"",
            }
        ],
        ""_comment"": """"
    }
]";
        
        private const string findingOnlyRule = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.MallocNotFree1"",
        ""tags"": [
            ""Testing.Rules.MallocNotFree1""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule aims to find malloc() that does NOT have free() in 1 line range"",
        ""patterns"": [
            {
                ""pattern"": ""racecar"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""modifiers"": [
                    ""i""
                ],
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""conditions"": [
            {
                ""pattern"": {
                    ""pattern"": ""car"",
                    ""type"": ""regex"",
                    ""scopes"": [
                        ""code""
                    ]
                },
                ""search_in"": ""finding-only"",
                ""negate_finding"": REPLACE_NEGATE
            }
        ],
        ""_comment"": """"
    }
]";

        private const string findingRangeZeroRule = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.MallocNotFree1"",
        ""tags"": [
            ""Testing.Rules.MallocNotFree1""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule aims to find malloc() that does NOT have free() in 1 line range"",
        ""patterns"": [
            {
                ""pattern"": ""racecar"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""modifiers"": [
                    ""i""
                ],
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""conditions"": [
            {
                ""pattern"": {
                    ""pattern"": ""car"",
                    ""type"": ""regex"",
                    ""scopes"": [
                        ""code""
                    ]
                },
                ""search_in"": ""finding-region(0,0)"",
                ""negate_finding"": REPLACE_NEGATE
            }
        ],
        ""_comment"": """"
    }
]";

        const string insideFindingData = @"
raceCAR
racecar";

        const string findingRangeData = @"#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>

#define BUFSIZER1 512
#define BUFSIZER2 ((BUFSIZER1 / 2) - 8)

int main(int argc, char **argv)
{
    char *buf1R1;
    char *buf2R1;
    buf1R1 = (char *)malloc(BUFSIZER1);
    buf2R1 = (char *)malloc(BUFSIZER1);
    free(buf2R1);
    strncpy(buf2R1, argv[1], BUFSIZER1 - 1);
    free(buf1R1);
}";

        const string sameLineData = @"#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>

#define BUFSIZER1 512
#define BUFSIZER2 ((BUFSIZER1 / 2) - 8)

int main(int argc, char **argv)
{
    char *buf1R1;
    char *buf2R1;
    buf1R1 = (char *)malloc(BUFSIZER1);
    buf2R1 = (char *)malloc(BUFSIZER1);free(buf2R1);
    
    strncpy(buf2R1, argv[1], BUFSIZER1 - 1);
    free(buf1R1);
}";
        
        const string multiLineData = @"
    buf2R1 = (char *)malloc(BUFSIZER1);
    free
();";
        private const string baseRule = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.MallocNotFree1"",
        ""tags"": [
            ""Testing.Rules.MallocNotFree1""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule aims to find malloc() that does NOT have free() in 1 line range"",
        ""patterns"": [
            {
                ""pattern"": ""malloc\\("",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""modifiers"": [
                    ""i""
                ],
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""conditions"": [
            {
                ""pattern"": {
                    ""pattern"": ""free\\("",
                    ""type"": ""regex"",
                    ""scopes"": [
                        ""code""
                    ],
                    ""modifiers"": [
                        ""i""
                    ]
                },
                ""search_in"": ""REPLACE_REGION"",
                ""negate_finding"": REPLACE_NEGATE
            }
        ],
        ""_comment"": """"
    }
]";
        private const string multiLineRule = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.MallocNotFree1"",
        ""tags"": [
            ""Testing.Rules.MallocNotFree1""
        ],
        ""severity"": ""Critical"",
        ""description"": ""this rule aims to find malloc() that does NOT have free\\r\\n() in 1 line range"",
        ""patterns"": [
            {
                ""pattern"": ""malloc\\("",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""modifiers"": [
                    ""i""
                ],
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""conditions"": [
            {
                ""pattern"": {
                    ""pattern"": ""free\\r\\n\\("",
                    ""type"": ""regex"",
                    ""scopes"": [
                        ""code""
                    ],
                    ""modifiers"": [
                        ""i"",
                        ""m""
                    ]
                },
                ""search_in"": ""finding_region(-3,3)""
            }
        ],
        ""_comment"": """"
    }
]";
    }
}
