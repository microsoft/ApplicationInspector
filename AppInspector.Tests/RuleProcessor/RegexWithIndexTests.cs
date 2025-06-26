using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

[ExcludeFromCodeCoverage]
public class RegexWithIndexTests
{
    private const string jsonAndXmlRule = @"[
        {
            ""id"": ""SA000005"",
            ""name"": ""Testing.Rules.JSONandXML"",
            ""tags"": [
                ""Testing.Rules.JSON.JSONandXML""
            ],
            ""severity"": ""Critical"",
            ""description"": ""This rule finds books from the JSON or XML titled with Franklin."",
            ""patterns"": [
                {
                    ""pattern"": ""Franklin"",
                    ""type"": ""regex"",
                    ""confidence"": ""High"",
                    ""scopes"": [
                        ""code""
                    ],
                    ""jsonpaths"" : [""$.books[*].title""],
                    ""xpaths"" : [""/bookstore/book/title""]
                }
            ],
            ""_comment"": """"
        }
    ]";

    private const string jsonRule = @"[
        {
            ""id"": ""SA000005"",
            ""name"": ""Testing.Rules.JSON"",
            ""tags"": [
                ""Testing.Rules.JSON""
            ],
            ""severity"": ""Critical"",
            ""description"": ""This rule finds books from the JSON titled with Franklin."",
            ""patterns"": [
                {
                    ""pattern"": ""Franklin"",
                    ""type"": ""regex"",
                    ""confidence"": ""High"",
                    ""scopes"": [
                        ""code""
                    ],
                    ""jsonpaths"" : [""$.books[*].title""]
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
                ""xpaths"" : [""/bookstore/book/title""]
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
        },
        {
            ""category"": ""memoir"",
            ""title"" : ""The Autobiography of Benjamin Franklin"",
            ""author"" : ""Benjamin Franklin"",
            ""price"" : 123.45
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
                ""pattern"": ""race\\r?\\ncar"",
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
                ""pattern"": ""race\\r?\\ncar"",
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

    private const string multiLineData = @"
race
CAR
race
car";

    private const string singleLineData = @"
raceCAR
racecar";

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();


    [Fact]
    public void NoDictDataAllowed()
    {
        RuleSet rules = new();
        rules.AddString(multiLineRule, "TestRules");
        var theRule = rules.GetOatRules().First();
        theRule.Clauses.First().DictData = new List<KeyValuePair<string, string>>
            { new KeyValuePair<string, string>("test", "test") };

        Analyzer analyzer = new ApplicationInspectorAnalyzer();
        var issues = analyzer.EnumerateRuleIssues(theRule);

        Assert.Single(issues);
    }

    [Fact]
    public void NoData()
    {
        RuleSet rules = new();
        rules.AddString(multiLineRule, "TestRules");
        var theRule = rules.GetOatRules().First();
        theRule.Clauses.First().Data = new List<string>();

        Analyzer analyzer = new ApplicationInspectorAnalyzer();
        var issues = analyzer.EnumerateRuleIssues(theRule);

        Assert.Single(issues);
    }

    [Fact]
    public void InvalidRegex()
    {
        RuleSet rules = new();
        rules.AddString(multiLineRule, "TestRules");
        var theRule = rules.GetOatRules().First();
        theRule.Clauses.First().Data = new List<string> { "^($" };

        Analyzer analyzer = new ApplicationInspectorAnalyzer();
        var issues = analyzer.EnumerateRuleIssues(theRule);

        Assert.Single(issues);
    }

    [Fact]
    public void InvalidRegexWhenAnalyzing()
    {
        RuleSet rules = new();
        rules.AddString(multiLineRule, "TestRules");
        var theRule = rules.GetOatRules().First();
        theRule.Clauses.First().Data = new List<string> { "^($" };

        Analyzer analyzer = new ApplicationInspectorAnalyzer();
        var issues = analyzer.Analyze(rules.GetOatRules(),
            new TextContainer("TestContent", "csharp", new Microsoft.ApplicationInspector.RulesEngine.Languages()));

        Assert.Empty(issues);
    }

    [Fact]
    public void MultiLine()
    {
        RuleSet rules = new();
        rules.AddString(multiLineRule, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
        if (_languages.FromFileNameOut("test.c", out var info))
        {
            var matches = processor.AnalyzeFile(multiLineData, new FileEntry("test.cs", new MemoryStream()), info);
            Assert.Single(matches);
        }
        else
        {
            Assert.Fail();
        }
    }

    [Fact]
    public void MultiLineCaseInsensitive()
    {
        RuleSet rules = new();
        rules.AddString(multiLineCaseInsensitiveRule, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
        if (_languages.FromFileNameOut("test.c", out var info))
        {
            var matches = processor.AnalyzeFile(multiLineData, new FileEntry("test.cs", new MemoryStream()), info);
            Assert.Equal(2, matches.Count);
        }
        else
        {
            Assert.Fail();
        }
    }

    [Fact]
    public void MultiLineRuleWithSingleLineData()
    {
        RuleSet rules = new();
        rules.AddString(multiLineRule, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
        if (_languages.FromFileNameOut("test.c", out var info))
        {
            var matches = processor.AnalyzeFile(singleLineData, new FileEntry("test.cs", new MemoryStream()), info);
            Assert.Empty(matches);
        }
        else
        {
            Assert.Fail();
        }
    }

    [Fact]
    public void MultiLineRuleWithoutOptionSet()
    {
        RuleSet rules = new();
        rules.AddString(multiLineRuleWithoutMultiLine, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
        if (_languages.FromFileNameOut("test.c", out var info))
        {
            var matches = processor.AnalyzeFile(singleLineData, new FileEntry("test.cs", new MemoryStream()), info);
            Assert.Empty(matches);
        }
        else
        {
            Assert.Fail();
        }
    }

    [InlineData(jsonRule)]
    [InlineData(jsonAndXmlRule)]
    [Theory]
    public void JsonRule(string rule)
    {
        RuleSet rules = new();
        rules.AddString(rule, "JsonTestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.json", out var info))
        {
            var matches = processor.AnalyzeFile(jsonData, new FileEntry("test.json", new MemoryStream()), info);
            Assert.Single(matches);
        }
        else
        {
            Assert.Fail();
        }
    }

    [InlineData(xmlRule)]
    [InlineData(jsonAndXmlRule)]
    [Theory]
    public void XmlRule(string rule)
    {
        RuleSet rules = new();
        rules.AddString(rule, "XmlTestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xmlData, new FileEntry("test.xml", new MemoryStream()), info);
            Assert.Single(matches);
        }
        else
        {
            Assert.Fail();
        }
    }
}