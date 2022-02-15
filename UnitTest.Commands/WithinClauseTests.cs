namespace UnitTest.Commands
{
    using Microsoft.ApplicationInspector.RulesEngine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    [TestClass]
    public class WithinClauseTests
    {
        [TestMethod]
        public void WithinClauseWithInvert()
        {
            string testData = @"#include <stdio.h>
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
            string testRules = @"[
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
                ""search_in"": ""finding-region(0,1)"",
                ""negate_finding"": true
            }
        ],
        ""_comment"": """"
    }
]";
            RuleSet rules = new(null);
            rules.AddString(testRules, "WithinClauseWithInverTestRules");
            RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (Language.FromFileNameOut("test.cs", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(testData, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
                Assert.AreEqual(1, matches.Count);
                Assert.AreEqual(13, matches[0].StartLocationLine);
            }
            else
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void WithinClauseWithoutInvert()
        {
            string testData = @"#include <stdio.h>
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
            string testRules = @"[
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
                ""search_in"": ""finding-region(0,1)"",
                ""negate_finding"": false
            }
        ],
        ""_comment"": """"
    }
]";
            RuleSet rules = new(null);
            rules.AddString(testRules, "WithinClauseWithInverTestRules");
            RuleProcessor processor = new(rules, new RuleProcessorOptions());
            if (Language.FromFileNameOut("test.cs", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(testData, new Microsoft.CST.RecursiveExtractor.FileEntry("test.cs", new MemoryStream()), info);
                Assert.AreEqual(1, matches.Count);
                Assert.AreEqual(14, matches[0].StartLocationLine);
            }
            else
            {
                Assert.Fail();
            }
        }
    }
}
