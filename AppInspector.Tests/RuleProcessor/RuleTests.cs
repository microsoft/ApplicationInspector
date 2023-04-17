using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.CST.RecursiveExtractor.FileEntry;

namespace AppInspector.Tests.RuleProcessor;

[TestClass]
public class RuleTests
{
    private const string MultiLineRuleWithoutMultiLine = @"[
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

    [TestMethod]
    public void ModifySource()
    {
        RuleSet rules = new();
        var originalSource = "TestRules";
        rules.AddString(MultiLineRuleWithoutMultiLine, originalSource);
        var rule = rules.First();
        Assert.AreEqual(originalSource, rule.Source);
        var newSource = "Somewhere";
        rule.Source = newSource;
        Assert.AreEqual(newSource, rule.Source);
    }

    [TestMethod]
    public void ModifyRuntimeTag()
    {
        RuleSet rules = new();
        var originalSource = "TestRules";
        rules.AddString(MultiLineRuleWithoutMultiLine, originalSource);
        var rule = rules.First();
        var newTag = "SomeTag";
        rule.RuntimeTag = newTag;
        Assert.AreEqual(newTag, rule.RuntimeTag);
    }

    [TestMethod]
    public void ModifyDisabled()
    {
        RuleSet rules = new();
        var originalSource = "TestRules";
        rules.AddString(MultiLineRuleWithoutMultiLine, originalSource);
        var rule = rules.First();
        rule.Disabled = true;
        Assert.AreEqual(true, rule.Disabled);
    }

    private const string overrideRules = @"[
    {
        ""id"": ""SA000005"",
        ""name"": ""Testing.Rules.Overridee"",
        ""tags"": [
            ""Testing.Rules.Overridee""
        ],
        ""severity"": ""Critical"",
        ""description"": ""This rule finds car"",
        ""patterns"": [
            {
                ""pattern"": ""car"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    },
    {
        ""id"": ""SA000006"",
        ""name"": ""Testing.Rules.Overridee"",
        ""tags"": [
            ""Testing.Rules.Overridee""
        ],
        ""overrides"": [""SA000005""],
        ""severity"": ""Critical"",
        ""description"": ""This rule finds racecar"",
        ""patterns"": [
            {
                ""pattern"": ""racecar"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    },
    {
        ""id"": ""SA000007"",
        ""name"": ""Testing.Rules.Overridee"",
        ""tags"": [
            ""Testing.Rules.Overridee""
        ],
        ""overrides"": [""SA000005""],
        ""severity"": ""Critical"",
        ""description"": ""This rule finds ar"",
        ""patterns"": [
            {
                ""pattern"": ""ar"",
                ""type"": ""regex"",
                ""confidence"": ""High"",
                ""scopes"": [
                    ""code""
                ]
            }
        ],
        ""_comment"": """"
    }
]
";
    
    [TestMethod]
    public async Task Overrides()
    {
        RuleSet rules = new();
        var originalSource = "TestRules";
        rules.AddString(overrideRules, originalSource);
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules, new RuleProcessorOptions());
        var entry = await FromStreamAsync("dummy", new MemoryStream(Encoding.UTF8.GetBytes("racecar car")));
        var langs = new Microsoft.ApplicationInspector.RulesEngine.Languages();
        langs.FromFileNameOut("dummy.cs", out LanguageInfo info);
        var results = processor.AnalyzeFile(entry, info);
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual(1, results.Count(x=> x.Rule.Id == "SA000006"));
        Assert.AreEqual(1, results.Count(x=> x.Rule.Id == "SA000005"));
        Assert.AreEqual(2, results.Count(x=> x.Rule.Id == "SA000007"));
    }
}