using System;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Events;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

public class LoadRulesTests
{
    private static readonly ILoggerFactory _loggerFactory =
        new LogOptions { ConsoleVerbosityLevel = LogEventLevel.Verbose }.GetLoggerFactory();

    private static ILogger _logger = new NullLogger<WithinClauseTests>();

    private static string multiLineRuleLoc = string.Empty;
    private static string multiLineRuleLoc2 = string.Empty;

    public LoadRulesTests()
    {
        _logger = _loggerFactory.CreateLogger<LoadRulesTests>();
        multiLineRuleLoc = Path.Combine("TestData", "TestRuleProcessor","Rules",
            "MultiLineRule.json");
        multiLineRuleLoc2 = Path.Combine("TestData", "TestRuleProcessor","Rules",
            "MultiLineRule2.json");
    }

    [Fact]
    public void AddFileByPath()
    {
        RuleSet rules = new(_loggerFactory);

        rules.AddPath(multiLineRuleLoc, "multiline-tests");
        Assert.Single(rules);
    }

    [Fact]
    public void AddDirectoryByPath()
    {
        RuleSet rules = new(_loggerFactory);
        rules.AddPath(
            Path.GetDirectoryName(multiLineRuleLoc) ?? throw new ArgumentNullException(nameof(multiLineRuleLoc)),
            "multiline-tests");
        Assert.Equal(2, rules.Count());
    }

    [Fact]
    public void AddInvalidPath()
    {
        RuleSet rules = new(_loggerFactory);
        var multiLineRuleLoc = "ThisIsDefinitelyNotADirectoryThatExists";
        Assert.Throws<ArgumentException>(() => rules.AddPath(multiLineRuleLoc, "multiline-tests"));
    }
}