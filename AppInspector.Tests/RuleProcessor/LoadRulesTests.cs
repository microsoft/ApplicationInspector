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
    private static readonly ILoggerFactory _loggerFactory =
        new LogOptions { ConsoleVerbosityLevel = LogEventLevel.Verbose }.GetLoggerFactory();

    private static ILogger _logger = new NullLogger<WithinClauseTests>();

    private static string multiLineRuleLoc = string.Empty;
    private static string multiLineRuleLoc2 = string.Empty;

    [ClassInitialize]
    public static void TestInit(TestContext testContext)
    {
        _logger = _loggerFactory.CreateLogger<LoadRulesTests>();
        Directory.CreateDirectory(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "LoadRuleTests"));
        multiLineRuleLoc = Path.Combine("TestData", "TestRuleProcessor","Rules",
            "MultiLineRule.json");
        multiLineRuleLoc2 = Path.Combine("TestData", "TestRuleProcessor","Rules",
            "MultiLineRule2.json");
    }

    [ClassCleanup]
    public static void TestCleanup()
    {
        Directory.Delete(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "LoadRuleTests"), true);
    }

    [TestMethod]
    public void AddFileByPath()
    {
        RuleSet rules = new(_loggerFactory);

        rules.AddPath(multiLineRuleLoc, "multiline-tests");
        Assert.AreEqual(1, rules.Count());
    }

    [TestMethod]
    public void AddDirectoryByPath()
    {
        RuleSet rules = new(_loggerFactory);
        rules.AddPath(
            Path.GetDirectoryName(multiLineRuleLoc) ?? throw new ArgumentNullException(nameof(multiLineRuleLoc)),
            "multiline-tests");
        Assert.AreEqual(2, rules.Count());
    }

    [TestMethod]
    public void AddInvalidPath()
    {
        RuleSet rules = new(_loggerFactory);
        var multiLineRuleLoc = "ThisIsDefinitelyNotADirectoryThatExists";
        Assert.ThrowsException<ArgumentException>(() => rules.AddPath(multiLineRuleLoc, "multiline-tests"));
    }
}