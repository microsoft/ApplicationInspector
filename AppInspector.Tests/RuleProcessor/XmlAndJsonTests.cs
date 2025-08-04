using System.IO;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

public class XmlAndJsonTests
{
    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    private const string jsonAndXmlStringRule = @"[
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
                    ""type"": ""string"",
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

    private const string jsonStringRule = @"[
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
                    ""type"": ""string"",
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
                ""xpaths"" : [""/bookstore/book/title""]
            }
        ],
        ""_comment"": """"
    }
]";

    private const string xmlStringRuleForPropWithData = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.XML"",
        ""tags"": [
            ""Testing.Rules.XML""
        ],
        ""severity"": ""Critical"",
        ""description"": ""This rule checks the value of the property property to be true"",
        ""patterns"": [
            {
                ""pattern"": ""true"",
                ""type"": ""string"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ],
                ""xpaths"" : [""/bookstore/book/title/@*[name()='property']""]
            }
        ],
        ""_comment"": """"
    }
]";

    private const string xmlStringRuleForPropWithDataForData = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.XML"",
        ""tags"": [
            ""Testing.Rules.XML""
        ],
        ""severity"": ""Critical"",
        ""description"": ""This rule checks the value of the title tag when it has a property"",
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

    private const string xmlDataPropsWithTagValue =
        @"<?xml version=""1.0"" encoding=""utf-8"" ?>   
  <bookstore>  
      <book genre=""autobiography"" publicationdate=""1981-03-22"" ISBN=""1-861003-11-0"">  
          <title property=""true"">The Autobiography of Benjamin Franklin</title>  
          <author>  
              <first-name>Benjamin</first-name>  
              <last-name>Franklin</last-name>  
          </author>  
          <price>8.99</price>  
      </book>  
      <book genre=""novel"" publicationdate=""1967-11-17"" ISBN=""0-201-63361-2"">  
          <title property=""false"">The Confidence Man</title>  
          <author>  
              <first-name>Herman</first-name>  
              <last-name>Melville</last-name>  
          </author>  
          <price>11.99</price>  
      </book>  
      <book genre=""philosophy"" publicationdate=""1991-02-15"" ISBN=""1-861001-57-6"">  
          <title property=""false"">The Gorgias</title>  
          <author>  
              <name>Plato</name>  
          </author>  
          <price>9.99</price>  
      </book>  
  </bookstore>";

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

    private const string NamespacedXmlData = @"
[
    {
        ""name"": ""Android debug is enabled."",
        ""id"": ""DS180000"",
        ""description"": ""The android:debuggable element is set to true, which should be disabled for release builds."",
        ""recommendation"": ""Set android:debuggable to false for release builds."",
        ""applies_to_file_regex"": [
            ""AndroidManifest.xml""
        ],
        ""tags"": [
            ""Framework.Android""
        ],
        ""severity"": ""BestPractice"",
        ""rule_info"": ""DS180000.md"",
        ""patterns"": [
            {
                ""xpaths"": [""//default:application/@android:debuggable""],
                ""pattern"": ""true"",
                ""type"": ""regex"",
                ""scopes"": [
                    ""code""
                ],
                ""modifiers"" : [""i""],
                ""xpathnamespaces"": {
                    ""default"": ""http://maven.apache.org/POM/4.0.0"",
                    ""android"": ""http://schemas.android.com/apk/res/android""
                }
            }
        ],
        ""must-match"": [
            ""<?xml version=\""1.0\"" encoding=\""utf-8\""?><manifest xmlns=\""http://maven.apache.org/POM/4.0.0\"" xmlns:android=\""http://schemas.android.com/apk/res/android\""><application android:debuggable='true' /></manifest>""
        ],
        ""must-not-match"": [
            ""<?xml version=\""1.0\"" encoding=\""utf-8\""?><manifest xmlns=\""http://maven.apache.org/POM/4.0.0\"" xmlns:android=\""http://schemas.android.com/apk/res/android\""><application android:debuggable='false' /></manifest>""
        ]
    }
]";

    [Fact]
    public void XmlWithNamespaces()
    {
        RuleSet rules = new();
        rules.AddString(NamespacedXmlData, "JsonTestRules");
        RulesVerifier verifier = new RulesVerifier(new RulesVerifierOptions());
        //var verification= verifier.Verify(rules);
        //Assert.Equal(true,verification.Verified);
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("AndroidManifest.xml", out var info))
        {
            var matches = processor.AnalyzeFile(@"<?xml version=""1.0"" encoding=""utf-8""?><manifest xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:android=""http://schemas.android.com/apk/res/android""><application android:debuggable='true' /></manifest>", new FileEntry("AndroidManifest.xml", new MemoryStream()), info);
            Assert.Single(matches);
        }
    }

    [Fact]
    public void XmlAttributeTest()
    {
        var attributeContent =
            "<system.web>\n<trace enabled='true' pageOutput='false' requestLimit='40' localOnly='false' />\n</system.web>";
        var attributeRule = @"[{
        ""name"": ""Trace is enabled in system.web"",
        ""id"": ""DS450002"",
        ""description"": ""Having traces enabled could leak sensitive application information in production."",
        ""recommendation"": ""Disable tracing before deploying to production."",
		""applies_to_file_regex"": [
			"".*\\.config""
		],
        ""tags"": [
            ""Framework.NET""
        ],
        ""severity"": ""important"",
        ""rule_info"": ""DS450002.md"",
        ""patterns"": [
            {
                ""xpaths"": [""system.web/trace/@enabled""],
                ""pattern"": ""true"",
                ""type"": ""string""
            }
        ],
        ""must-match"": [
            ""<system.web>\n<trace enabled='true' pageOutput='false' requestLimit='40' localOnly='true' />\n</system.web>""
        ],
        ""must-not-match"": [
            ""<system.web>\n<trace enabled='false' pageOutput='false' requestLimit='40' localOnly='true' />\n</system.web>""
        ]
    }]";
        RuleSet rules = new();
        rules.AddString(attributeRule, "JsonTestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.config", out var info))
        {
            var matches = processor.AnalyzeFile(attributeContent, new FileEntry("test.config", new MemoryStream()), info);
            Assert.Single(matches);
        }
    }
    
    [InlineData(jsonStringRule)]
    [InlineData(jsonAndXmlStringRule)]
    [Theory]
    public void JsonStringRule(string rule)
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
    [InlineData(xmlStringRuleForPropWithDataForData, "Franklin", 209)]
    [InlineData(xmlStringRuleForPropWithData, "true", 173)]
    [Theory]
    public void XmlTagWithPropsAndValue(string rule, string expectedValue, int expectedIndex)
    {
        RuleSet rules = new();
        rules.AddString(rule, "XmlTestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xmlDataPropsWithTagValue.ReplaceLineEndings("\n"), new FileEntry("test.xml", new MemoryStream()), info);
            Assert.Single(matches);
            var match = matches[0];
            Assert.Equal(expectedValue, match.Sample);
            Assert.Equal(expectedIndex, match.Boundary.Index);
        }
        else
        {
            Assert.Fail();
        }
    }

    [InlineData(xmlStringRule)]
    [InlineData(jsonAndXmlStringRule)]
    [Theory]
    public void XmlStringRule(string rule)
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

    [Fact]
    public void JsonBooleanRule()
    {
        var testContent = @"{
    ""list"":
    [
        {
            ""field1"": ""Foo"",
            ""field2"": ""Bar"",
            ""field3"": false
        },
        {
            ""field1"": ""Contoso"",
            ""field2"": ""Elephant"",
            ""field3"": true
        }
    ]
}";
        var testRule = @"[
    {
        ""id"": ""Field3true"",
        ""name"": ""Testing.Rules.JSON"",
        ""tags"": [
            ""Testing.Rules.JSON""
        ],
        ""severity"": ""Critical"",
        ""confidence"": ""High"",
        ""description"": ""This rule finds field3 is true"",
        ""patterns"": [
            {
                ""pattern"": ""true"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""jsonpaths"" : [""$.list[*].field3""]
            }
        ]
    }
]";
        RuleSet rules = new();
        var originalSource = "TestRules";
        rules.AddString(testRule, originalSource);
        var analyzer = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules,
            new RuleProcessorOptions { Parallel = false, AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.json", out var info))
        {
            var matches = analyzer.AnalyzeFile(testContent.ReplaceLineEndings("\n"), new FileEntry("test.json", new MemoryStream()), info);
            Assert.Single(matches);
            Assert.Equal(226, matches[0].Boundary.Index);
            Assert.Equal("true",matches[0].Sample);
        }
    }

    [Fact]
    public void TestYml()
    {
        var content =@"hash_name:
  a_key: 0
  b_key: 1
  c_key: 2
  d_key: 3
  e_key: 4";
        var ruleThatWontFind = @"[{
    ""name"": ""YamlPathValidate"",
    ""id"": ""YmlPath"",
    ""description"": ""Yaml Path Validation"",
    ""tags"": [
      ""Code.Java.17""
    ],
    ""severity"": ""critical"",
    ""patterns"": [
      {
        ""pattern"": ""3"",
        ""ymlpaths"" : [""/hash_name/b_key""],
        ""type"": ""string"",
        ""scopes"": [
          ""code""
        ],
        ""modifiers"": [
          ""i""
        ],
        ""confidence"": ""high""
      }
    ]
  }]";
        var ruleWithRegex = @"[{
    ""name"": ""YamlPathValidate"",
    ""id"": ""YmlPath"",
    ""description"": ""Yaml Path Validation"",
    ""tags"": [
      ""Code.Java.17""
    ],
    ""severity"": ""critical"",
    ""patterns"": [
      {
        ""pattern"": ""0"",
        ""ymlpaths"" : [""/hash_name/a_key""],
        ""type"": ""regex"",
        ""scopes"": [
          ""code""
        ],
        ""modifiers"": [
          ""i""
        ],
        ""confidence"": ""high""
      }
    ]
  }]";
        var rule = @"[{
    ""name"": ""YamlPathValidate"",
    ""id"": ""YmlPath"",
    ""description"": ""Yaml Path Validation"",
    ""tags"": [
      ""Code.Java.17""
    ],
    ""severity"": ""critical"",
    ""patterns"": [
      {
        ""pattern"": ""0"",
        ""ymlpaths"" : [""/hash_name/a_key""],
        ""type"": ""string"",
        ""scopes"": [
          ""code""
        ],
        ""modifiers"": [
          ""i""
        ],
        ""confidence"": ""high""
      }
    ]
  }]";
        // This rule should find one match
        RuleSet rules = new();
        var originalSource = "TestRules";
        rules.AddString(rule, originalSource);
        var analyzer = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules,
            new RuleProcessorOptions { Parallel = false, AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.yml", out var info))
        {
            var matches = analyzer.AnalyzeFile(content, new FileEntry("test.yml", new MemoryStream()), info);
            Assert.Single(matches);
        }
        rules = new();
        
        // This rule intentionally does not find a match
        rules.AddString(ruleThatWontFind, originalSource);
        analyzer = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules,
            new RuleProcessorOptions { Parallel = false, AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.yml", out var info2))
        {
            var matches = analyzer.AnalyzeFile(content, new FileEntry("test.yml", new MemoryStream()), info2);
            Assert.Empty(matches);
        }
        rules = new();
        
        // This is the same rule as the first but with the regex operation
        rules.AddString(ruleWithRegex, originalSource);
        analyzer = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules,
            new RuleProcessorOptions { Parallel = false, AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.yml", out var info3))
        {
            var matches = analyzer.AnalyzeFile(content, new FileEntry("test.yml", new MemoryStream()), info3);
            Assert.Single(matches);
        }
    }

    [Fact]
    public void TestMultiDocYaml()
    {
        string content =
            @"
---
test:
  test1:
    - something
    - other
  test2: true
  test3:
    test:
      other:
        - other1:
          property1: 1
          property2: 2
      tested: ok
---
test:
  test1:
    - something
    - other
  test2: true
  test3:
    test:
      other:
        - other1:
          property1: 1
          property2: 2
      tested: ok
";
        string rule =
            @"[
    {
      ""name"": ""Yaml test"",
        ""id"": ""00000001"",
        ""applies_to_file_regex"": [
        ""test.yml""
            ],
        ""tags"": [
        ""MyTest""
            ],
        ""severity"": ""moderate"",
        ""patterns"": [
        {
            ""pattern"": ""ok"",
            ""ymlpaths"": [""/test/test3/test/tested""],
            ""type"": ""string"",
            ""scopes"": [
            ""code""
                ],
            ""modifiers"": [
            ""m""
                ],
            ""confidence"": ""high""
        }
        ]
    }
    ]";
        string rule2 =
            @"[
    {
      ""name"": ""Yaml test"",
        ""id"": ""00000001"",
        ""applies_to_file_regex"": [
        ""test.yml""
            ],
        ""tags"": [
        ""MyTest""
            ],
        ""severity"": ""moderate"",
        ""patterns"": [
        {
            ""pattern"": ""ok"",
            ""ymlpaths"": [""/test/test3/test/tested""],
            ""type"": ""regex"",
            ""scopes"": [
            ""code""
                ],
            ""modifiers"": [
            ""m""
                ],
            ""confidence"": ""high""
        }
        ]
    }
    ]";
        string standardizedContent = content.ReplaceLineEndings("\n");
        RuleSet rules = new();
        var originalSource = "TestRules";
        rules.AddString(rule, originalSource);
        RuleSet rules2 = new();
        rules2.AddString(rule2, originalSource);
        var analyzer = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules,
            new RuleProcessorOptions { Parallel = false, AllowAllTagsInBuildFiles = true });
        var analyzer2 = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules2,
            new RuleProcessorOptions { Parallel = false, AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.yml", out var info))
        {
            var matches = analyzer.AnalyzeFile(standardizedContent, new FileEntry("test.yml", new MemoryStream()), info);
            Assert.Equal(2, matches.Count);
            Assert.Equal("ok", matches[0].Sample);
            Assert.Equal(172, matches[0].Boundary.Index);
            Assert.Equal(2, matches[0].Boundary.Length);
            Assert.Equal(346, matches[1].Boundary.Index);
            Assert.Equal(2, matches[1].Boundary.Length);
            var matches2 = analyzer2.AnalyzeFile(standardizedContent, new FileEntry("test.yml", new MemoryStream()), info);
            Assert.Equal(2, matches2.Count);
            Assert.Equal("ok", matches2[0].Sample);
            Assert.Equal(172, matches[0].Boundary.Index);
            Assert.Equal(2, matches[0].Boundary.Length);
            Assert.Equal(346, matches[1].Boundary.Index);
            Assert.Equal(2, matches[1].Boundary.Length);
        }
    }

    [Fact]
    public void TestYamlWithIndexLocation()
    {
        string content =
            @"test:
  test1:
    - something
    - other
  test2: true
  test3:
    test:
      other:
        - other1:
          property1: 1
          property2: 2
      tested: ok
";
        string rule =
            @"[
    {
      ""name"": ""Yaml test"",
        ""id"": ""00000001"",
        ""applies_to_file_regex"": [
        ""test.yml""
            ],
        ""tags"": [
        ""MyTest""
            ],
        ""severity"": ""moderate"",
        ""patterns"": [
        {
            ""pattern"": ""ok"",
            ""ymlpaths"": [""/test/test3/test/tested""],
            ""type"": ""string"",
            ""scopes"": [
            ""code""
                ],
            ""modifiers"": [
            ""m""
                ],
            ""confidence"": ""high""
        }
        ]
    }
    ]";
        string rule2 =
            @"[
    {
      ""name"": ""Yaml test"",
        ""id"": ""00000001"",
        ""applies_to_file_regex"": [
        ""test.yml""
            ],
        ""tags"": [
        ""MyTest""
            ],
        ""severity"": ""moderate"",
        ""patterns"": [
        {
            ""pattern"": ""ok"",
            ""ymlpaths"": [""/test/test3/test/tested""],
            ""type"": ""regex"",
            ""scopes"": [
            ""code""
                ],
            ""modifiers"": [
            ""m""
                ],
            ""confidence"": ""high""
        }
        ]
    }
    ]";
        string standardizedContent = content.ReplaceLineEndings("\n");
        RuleSet rules = new();
        var originalSource = "TestRules";
        rules.AddString(rule, originalSource);
        RuleSet rules2 = new();
        rules2.AddString(rule2, originalSource);
        var analyzer = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules,
            new RuleProcessorOptions { Parallel = false, AllowAllTagsInBuildFiles = true });
        var analyzer2 = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules2,
            new RuleProcessorOptions { Parallel = false, AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.yml", out var info))
        {
            var matches = analyzer.AnalyzeFile(standardizedContent, new FileEntry("test.yml", new MemoryStream()), info);
            Assert.Single(matches);
            Assert.Equal("ok", matches[0].Sample);
            Assert.Equal(167, matches[0].Boundary.Index);
            Assert.Equal(2, matches[0].Boundary.Length);
            var matches2 = analyzer2.AnalyzeFile(standardizedContent, new FileEntry("test.yml", new MemoryStream()), info);
            Assert.Single(matches2);
            Assert.Equal("ok", matches2[0].Sample);
            Assert.Equal(167, matches[0].Boundary.Index);
            Assert.Equal(2, matches2[0].Boundary.Length);
        }
    }

    [Fact]
    public void TestXmlWithAndWithoutNamespace()
    {
        var content = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
  <modelVersion>4.0.0</modelVersion>

  <groupId>xxx</groupId>
  <artifactId>xxx</artifactId>
  <version>0.1.0-SNAPSHOT</version>
  <packaging>pom</packaging>

  <name>${project.groupId}:${project.artifactId}</name>
  <description />

  <properties>
    <java.version>17</java.version>
  </properties>

</project>";
        // The same as above but with no namespace specified
        var noNamespaceContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project>
  <modelVersion>4.0.0</modelVersion>

  <groupId>xxx</groupId>
  <artifactId>xxx</artifactId>
  <version>0.1.0-SNAPSHOT</version>
  <packaging>pom</packaging>

  <name>${project.groupId}:${project.artifactId}</name>
  <description />

  <properties>
    <java.version>17</java.version>
  </properties>

</project>";
        var rule = @"[{
    ""name"": ""Source code: Java 17"",
    ""id"": ""CODEJAVA000000"",
    ""description"": ""Java 17 maven configuration"",
    ""applies_to_file_regex"": [
      ""pom.xml""
    ],
    ""tags"": [
      ""Code.Java.17""
    ],
    ""severity"": ""critical"",
    ""patterns"": [
      {
        ""pattern"": ""17"",
        ""xpaths"" : [""/*[local-name(.)='project']/*[local-name(.)='properties']/*[local-name(.)='java.version']""],
        ""type"": ""regex"",
        ""scopes"": [
          ""code""
        ],
        ""modifiers"": [
          ""i""
        ],
        ""confidence"": ""high""
      }
    ]
  }]";
        RuleSet rules = new();
        var originalSource = "TestRules";
        rules.AddString(rule, originalSource);
        var analyzer = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules,
            new RuleProcessorOptions { Parallel = false, AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = analyzer.AnalyzeFile(content, new FileEntry("pom.xml", new MemoryStream()), info);
            Assert.Single(matches);
            matches = analyzer.AnalyzeFile(noNamespaceContent, new FileEntry("pom.xml", new MemoryStream()), info);
            Assert.Single(matches);
        }
    }

    [Fact]
    public void XPathComplexSampleExtractionBug()
    {
        // This test case reproduces the bug reported in https://github.com/microsoft/ApplicationInspector/issues/621
        // The issue is that when using complex XPath expressions, the sample and excerpt returned are from the beginning 
        // of the document instead of the matched content.
        
        var complexXPathRule = @"[
    {
        ""name"": ""TEST xpath complex"",
        ""description"": ""Detects the use of 1.x version of mylibrary"",
        ""id"": ""XPATH000001"",
        ""applies_to"": [
            ""pom.xml""
        ],
        ""tags"": [
            ""XPATH.Version""
        ],
        ""severity"": ""critical"",
        ""patterns"": [
            {
                ""pattern"": ""1\\..+"",
                ""type"": ""regex"",
                ""scopes"": [
                    ""code""
                ],
                ""modifiers"": [],
                ""confidence"": ""high"",
                ""xpaths"": [""//*[local-name(.)='artifactId' and text()='mylibrary']/parent::*/*[local-name(.)='version']""]
            }
        ]
    }
]";

        var complexPomXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
  <modelVersion>4.0.0</modelVersion>

  <artifactId>test</artifactId>

  <dependencyManagement>
    <dependencies>
      <dependency>
        <groupId>test.com</groupId>
        <artifactId>mylibrary</artifactId>
        <version>1.6.0</version>
      </dependency>
    </dependencies>
  </dependencyManagement>

</project>";

        RuleSet rules = new();
        rules.AddString(complexXPathRule, "XPathTestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = processor.AnalyzeFile(complexPomXml, new FileEntry("pom.xml", new MemoryStream()), info);
            Assert.Single(matches);
            
            var match = matches[0];
            
            // Test for correct behavior (this will fail until the bug is fixed)
            // BUG: https://github.com/microsoft/ApplicationInspector/issues/621
            // When using complex XPath expressions, the sample should contain the actual matched content
            // but currently returns content from the beginning of the document
            
            var expectedVersionPosition = complexPomXml.IndexOf("1.6.0");
            Assert.True(expectedVersionPosition > 0, "Version should be found in the document");
            
            // These assertions test the correct behavior that should happen once the bug is fixed
            Assert.Equal(expectedVersionPosition, match.Boundary.Index);
            Assert.Equal(5, match.Boundary.Length); // Length of "1.6.0"
            Assert.Equal("1.6.0", match.Sample);
        }
        else
        {
            Assert.Fail("Failed to get language info for pom.xml");
        }
    }

    [Fact]
    public void XPathComplexSampleExtractionBugStringPattern()
    {
        // This test case reproduces the second part of the bug reported in issue #621
        // Testing with string pattern type instead of regex
        
        var complexXPathRuleString = @"[
    {
        ""name"": ""TEST xpath complex string"",
        ""description"": ""Detects the use of 2.6.0 version of mylibrary"",
        ""id"": ""XPATH000002"",
        ""applies_to"": [
            ""pom.xml""
        ],
        ""tags"": [
            ""XPATH.Version""
        ],
        ""severity"": ""critical"",
        ""patterns"": [
            {
                ""pattern"": ""2.6.0"",
                ""type"": ""string"",
                ""scopes"": [
                    ""code""
                ],
                ""modifiers"": [],
                ""confidence"": ""high"",
                ""xpaths"": [""//*[local-name(.)='artifactId' and text()='mylibrary']/parent::*/*[local-name(.)='version']""]
            }
        ]
    }
]";

        var complexPomXmlVersion2 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
  <modelVersion>4.0.0</modelVersion>

  <artifactId>test</artifactId>

  <dependencyManagement>
    <dependencies>
      <dependency>
        <groupId>test.com</groupId>
        <artifactId>mylibrary</artifactId>
        <version>2.6.0</version>
      </dependency>
    </dependencies>
  </dependencyManagement>

</project>";

        RuleSet rules = new();
        rules.AddString(complexXPathRuleString, "XPathTestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = processor.AnalyzeFile(complexPomXmlVersion2, new FileEntry("pom.xml", new MemoryStream()), info);
            Assert.Single(matches);
            
            var match = matches[0];
            
            // Test for correct behavior (this will fail until the bug is fixed)
            // BUG: https://github.com/microsoft/ApplicationInspector/issues/621
            // When using complex XPath expressions, the sample should contain the actual matched content
            // but currently returns content from the beginning of the document
            
            var expectedVersionPosition = complexPomXmlVersion2.IndexOf("2.6.0");
            Assert.True(expectedVersionPosition > 0, "Version should be found in the document");
            
            // These assertions test the correct behavior that should happen once the bug is fixed
            Assert.Equal(expectedVersionPosition, match.Boundary.Index);
            Assert.Equal(5, match.Boundary.Length); // Length of "2.6.0"
            Assert.Equal("2.6.0", match.Sample);
        }
        else
        {
            Assert.Fail("Failed to get language info for pom.xml");
        }
    }

    [Fact]
    public void XPathComplexVersionsStartingWith2RegexPattern()
    {
        // This test verifies that regex patterns work correctly for versions starting with "2"
        // Related to the user's follow-up comment in GitHub issue #621
        
        var regexRuleFor2x = @"[
    {
        ""name"": ""TEST xpath 2.x versions regex"",
        ""description"": ""Detects the use of 2.x version of mylibrary using regex"",
        ""id"": ""XPATH000003"",
        ""applies_to"": [
            ""pom.xml""
        ],
        ""tags"": [
            ""XPATH.Version""
        ],
        ""severity"": ""critical"",
        ""patterns"": [
            {
                ""pattern"": ""2\\..+"",
                ""type"": ""regex"",
                ""scopes"": [
                    ""code""
                ],
                ""modifiers"": [],
                ""confidence"": ""high"",
                ""xpaths"": [""//*[local-name(.)='artifactId' and text()='mylibrary']/parent::*/*[local-name(.)='version']""]
            }
        ]
    }
]";

        var pomXmlWith2x = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
  <modelVersion>4.0.0</modelVersion>

  <artifactId>test</artifactId>

  <dependencyManagement>
    <dependencies>
      <dependency>
        <groupId>test.com</groupId>
        <artifactId>mylibrary</artifactId>
        <version>2.1.0</version>
      </dependency>
    </dependencies>
  </dependencyManagement>

</project>";

        RuleSet rules = new();
        rules.AddString(regexRuleFor2x, "XPathTestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = processor.AnalyzeFile(pomXmlWith2x, new FileEntry("pom.xml", new MemoryStream()), info);
            Assert.Single(matches);
            
            var match = matches[0];
            
            var expectedVersionPosition = pomXmlWith2x.IndexOf("2.1.0");
            Assert.True(expectedVersionPosition > 0, "Version should be found in the document");
            
            // Verify that the regex pattern correctly matches versions starting with "2"
            Assert.Equal(expectedVersionPosition, match.Boundary.Index);
            Assert.Equal(5, match.Boundary.Length); // Length of "2.1.0"
            Assert.Equal("2.1.0", match.Sample);
        }
        else
        {
            Assert.Fail("Failed to get language info for pom.xml");
        }
    }

    [Fact]
    public void XPathComplexVersionsStartingWith2StringPattern()
    {
        // This test verifies that string patterns work correctly for versions starting with "2"
        // Related to the user's follow-up comment in GitHub issue #621
        
        var stringRuleFor2x = @"[
    {
        ""name"": ""TEST xpath 2.x versions string"",
        ""description"": ""Detects the use of specific 2.x version of mylibrary using string"",
        ""id"": ""XPATH000004"",
        ""applies_to"": [
            ""pom.xml""
        ],
        ""tags"": [
            ""XPATH.Version""
        ],
        ""severity"": ""critical"",
        ""patterns"": [
            {
                ""pattern"": ""2.1.0"",
                ""type"": ""string"",
                ""scopes"": [
                    ""code""
                ],
                ""modifiers"": [],
                ""confidence"": ""high"",
                ""xpaths"": [""//*[local-name(.)='artifactId' and text()='mylibrary']/parent::*/*[local-name(.)='version']""]
            }
        ]
    }
]";

        var pomXmlWith2x = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
  <modelVersion>4.0.0</modelVersion>

  <artifactId>test</artifactId>

  <dependencyManagement>
    <dependencies>
      <dependency>
        <groupId>test.com</groupId>
        <artifactId>mylibrary</artifactId>
        <version>2.1.0</version>
      </dependency>
    </dependencies>
  </dependencyManagement>

</project>";

        RuleSet rules = new();
        rules.AddString(stringRuleFor2x, "XPathTestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = processor.AnalyzeFile(pomXmlWith2x, new FileEntry("pom.xml", new MemoryStream()), info);
            Assert.Single(matches);
            
            var match = matches[0];
            
            var expectedVersionPosition = pomXmlWith2x.IndexOf("2.1.0");
            Assert.True(expectedVersionPosition > 0, "Version should be found in the document");
            
            // Verify that the string pattern correctly matches versions starting with "2"
            Assert.Equal(expectedVersionPosition, match.Boundary.Index);
            Assert.Equal(5, match.Boundary.Length); // Length of "2.1.0"
            Assert.Equal("2.1.0", match.Sample);
        }
        else
        {
            Assert.Fail("Failed to get language info for pom.xml");
        }
    }

    [Fact]
    public void XPathMultipleVersionsComparison()
    {
        // This test verifies that our fix works correctly for multiple different version patterns
        // and that regex correctly distinguishes between different version ranges
        
        var multiVersionRule = @"[
    {
        ""name"": ""TEST xpath multiple versions"",
        ""description"": ""Detects various version patterns"",
        ""id"": ""XPATH000005"",
        ""applies_to"": [
            ""pom.xml""
        ],
        ""tags"": [
            ""XPATH.Version""
        ],
        ""severity"": ""critical"",
        ""patterns"": [
            {
                ""pattern"": ""[2-9]\\..+"",
                ""type"": ""regex"",
                ""scopes"": [
                    ""code""
                ],
                ""modifiers"": [],
                ""confidence"": ""high"",
                ""xpaths"": [""//*[local-name(.)='artifactId' and text()='mylibrary']/parent::*/*[local-name(.)='version']""]
            }
        ]
    }
]";

        var pomXmlWithMultipleVersions = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
  <modelVersion>4.0.0</modelVersion>

  <artifactId>test</artifactId>

  <dependencyManagement>
    <dependencies>
      <dependency>
        <groupId>test.com</groupId>
        <artifactId>otherlibrary</artifactId>
        <version>1.5.0</version>
      </dependency>
      <dependency>
        <groupId>test.com</groupId>
        <artifactId>mylibrary</artifactId>
        <version>2.3.1</version>
      </dependency>
      <dependency>
        <groupId>test.com</groupId>
        <artifactId>anotherlibrary</artifactId>
        <version>3.0.0</version>
      </dependency>
    </dependencies>
  </dependencyManagement>

</project>";

        RuleSet rules = new();
        rules.AddString(multiVersionRule, "XPathTestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = processor.AnalyzeFile(pomXmlWithMultipleVersions, new FileEntry("pom.xml", new MemoryStream()), info);
            Assert.Single(matches); // Should only match mylibrary with version 2.3.1, not otherlibrary with 1.5.0
            
            var match = matches[0];
            
            var expectedVersionPosition = pomXmlWithMultipleVersions.IndexOf("2.3.1");
            Assert.True(expectedVersionPosition > 0, "Version should be found in the document");
            
            // Verify that the regex correctly matches only mylibrary version 2.3.1
            Assert.Equal(expectedVersionPosition, match.Boundary.Index);
            Assert.Equal(5, match.Boundary.Length); // Length of "2.3.1"
            Assert.Equal("2.3.1", match.Sample);
            
            // Verify that 1.5.0 (otherlibrary) was not matched by ensuring our position is not at that location
            var otherLibraryVersionPosition = pomXmlWithMultipleVersions.IndexOf("1.5.0");
            Assert.NotEqual(otherLibraryVersionPosition, match.Boundary.Index);
        }
        else
        {
            Assert.Fail("Failed to get language info for pom.xml");
        }
    }
}