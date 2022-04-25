using System;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace AppInspector.Tests.RuleProcessor;

[TestClass]
public class LoadRulesTests
{
    [TestInitialize]
    public void TestInit()
    {
        _logger = _loggerFactory.CreateLogger<LoadRulesTests>();
        Directory.CreateDirectory(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "LoadRuleTests"));
    }
    
    [TestCleanup]
    public void TestCleanup()
    {
        Directory.Delete(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "LoadRuleTests"), true);
    }
    
    private readonly ILoggerFactory _loggerFactory = new LogOptions(){ ConsoleVerbosityLevel = LogEventLevel.Verbose }.GetLoggerFactory();
    private ILogger _logger = new NullLogger<WithinClauseTests>();

    [TestMethod]
    public void AddFileByPath()
    {
        RuleSet rules = new(_loggerFactory);
        string multiLineRuleLoc = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "LoadRuleTests", "multi_line_rule_file.json");
        File.WriteAllText(multiLineRuleLoc, multiLineRule);
        rules.AddPath(multiLineRuleLoc, "multiline-tests");
        Assert.AreEqual(1, rules.Count());
    }
    
    [TestMethod]
    public void AddDirectoryByPath()
    {
        RuleSet rules = new(_loggerFactory);
        string multiLineRuleLoc = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "LoadRuleTests", "multi_line_rule_file.json");
        File.WriteAllText(multiLineRuleLoc, multiLineRule);
        string rule2Loc = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "LoadRuleTests", "rule2_file.json");
        File.WriteAllText(rule2Loc, rule2);
        rules.AddPath(Path.GetDirectoryName(multiLineRuleLoc), "multiline-tests");
        Assert.AreEqual(2, rules.Count());
    }
    
    [TestMethod]
    public void AddInvalidPath()
    {
        RuleSet rules = new(_loggerFactory);
        string multiLineRuleLoc = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "ThisIsDefinitelyNotADirectoryThatExists");
        Assert.ThrowsException<ArgumentException>(() => rules.AddPath(multiLineRuleLoc, "multiline-tests"));
    }
    
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
    
    // Same rule as above but different id so both can be added
    private const string rule2 = @"[
    {
        ""id"": ""SA000006"",
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
}