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
        public void JsonSubstringRule()
        {
            RuleSet rules = new(null);
            rules.AddString(jsonStringRule, "JsonTestRules");
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
        public void XmlSubstringRule()
        {
            RuleSet rules = new(null);
            rules.AddString(xmlStringRule, "XmlTestRules");
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
        private const string jsonStringRule = @"[
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
                    ""type"": ""string"",
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
        
        private const string xmlStringRule = @"[
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
                ""type"": ""string"",
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