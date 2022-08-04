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
        
        [TestMethod]
        public void JsonRule()
        {
            RuleSet rules = new(null);
            rules.AddString(jsonRule, "JsonTestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions(){AllowAllTagsInBuildFiles = true});
            if (_languages.FromFileNameOut("test.json", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(jsonData, new Microsoft.CST.RecursiveExtractor.FileEntry("test.json", new MemoryStream()), info);
                Assert.AreEqual(1, matches.Count);
            }
            else
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void XmlRule()
        {
            RuleSet rules = new(null);
            rules.AddString(xmlRule, "XmlTestRules");
            Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions(){AllowAllTagsInBuildFiles = true});
            if (_languages.FromFileNameOut("test.xml", out LanguageInfo info))
            {
                List<MatchRecord> matches = processor.AnalyzeFile(xmlData, new Microsoft.CST.RecursiveExtractor.FileEntry("test.xml", new MemoryStream()), info);
                Assert.AreEqual(1, matches.Count);
            }
            else
            {
                Assert.Fail();
            }
        }

        private const string jsonRule = @"[
        {
            ""id"": ""SA000005"",
            ""name"": ""Testing.Rules.JSON"",
            ""tags"": [
                ""Testing.Rules.JSON""
            ],
            ""severity"": ""Critical"",
            ""description"": ""This rule finds books from the JSON titled with Sheep."",
            ""patterns"": [
                {
                    ""pattern"": ""Sheep"",
                    ""type"": ""regex"",
                    ""confidence"": ""High"",
                    ""scopes"": [
                        ""code""
                    ],
                    ""jsonpath"" : ""$.books[*].title""
                }
            ],
            ""_comment"": """"
        }
    ]";
        
        private const string xmlRule = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.XML"",
        ""tags"": [
            ""Testing.Rules.XML""
        ],
        ""severity"": ""Critical"",
        ""description"": ""This rule finds books from the XML titled with Franklin."",
        ""patterns"": [
            {
                ""pattern"": ""Franklin"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ],
                ""xpath"" : ""/bookstore/book/title""
            }
        ],
        ""_comment"": """"
    }
]";

        private const string jsonData = 
@"{
    ""books"":
    [
        {
            ""category"": ""fiction"",
            ""title"" : ""A Wild Sheep Chase"",
            ""author"" : ""Haruki Murakami"",
            ""price"" : 22.72
        },
        {
            ""category"": ""fiction"",
            ""title"" : ""The Night Watch"",
            ""author"" : ""Sergei Lukyanenko"",
            ""price"" : 23.58
        },
        {
            ""category"": ""fiction"",
            ""title"" : ""The Comedians"",
            ""author"" : ""Graham Greene"",
            ""price"" : 21.99
        },
        {
            ""category"": ""memoir"",
            ""title"" : ""The Night Watch"",
            ""author"" : ""David Atlee Phillips"",
            ""price"" : 260.90
        }
    ]
}
";
        
        private const string xmlData = 
@"<?xml version=""1.0"" encoding=""utf-8"" ?>   
  <bookstore>  
      <book genre=""autobiography"" publicationdate=""1981-03-22"" ISBN=""1-861003-11-0"">  
          <title>The Autobiography of Benjamin Franklin</title>  
          <author>  
              <first-name>Benjamin</first-name>  
              <last-name>Franklin</last-name>  
          </author>  
          <price>8.99</price>  
      </book>  
      <book genre=""novel"" publicationdate=""1967-11-17"" ISBN=""0-201-63361-2"">  
          <title>The Confidence Man</title>  
          <author>  
              <first-name>Herman</first-name>  
              <last-name>Melville</last-name>  
          </author>  
          <price>11.99</price>  
      </book>  
      <book genre=""philosophy"" publicationdate=""1991-02-15"" ISBN=""1-861001-57-6"">  
          <title>The Gorgias</title>  
          <author>  
              <name>Plato</name>  
          </author>  
          <price>9.99</price>  
      </book>  
  </bookstore>
";

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