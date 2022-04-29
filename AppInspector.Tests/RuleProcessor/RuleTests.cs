using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.RuleProcessor;

[TestClass]
public class RuleTests
{
    [TestMethod]
    public void ModifySource()
    {
        RuleSet rules = new(null);
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
        RuleSet rules = new(null);
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
        RuleSet rules = new(null);
        var originalSource = "TestRules";
        rules.AddString(MultiLineRuleWithoutMultiLine, originalSource);
        var rule = rules.First();
        rule.Disabled = true;
        Assert.AreEqual(true, rule.Disabled);
    }
    
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
}