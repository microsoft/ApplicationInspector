using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace AppInspector.Tests.DefaultRules;

/// <summary>
///     Tests for the default set of rules which are embedded in the executable.
/// </summary>
[TestClass]
[ExcludeFromCodeCoverage]
public class TestDefaultRules
{
    // This test ensures that the rules that are bundled with Application Inspector are valid.
    [TestMethod]
    public void VerifyDefaultRulesAreValid()
    {
        VerifyRulesOptions options = new()
        {
            VerifyDefaultRules = true
        };
        var loggerFactory = new LogOptions { ConsoleVerbosityLevel = LogEventLevel.Debug }.GetLoggerFactory();
        var logger = loggerFactory.CreateLogger("Tests");
        VerifyRulesCommand command = new(options, loggerFactory);
        var result = command.GetResult();

        if (result.Unverified.Any())
            logger.Log(LogLevel.Error, "{0} of {1} rules failed verification. Errors are as follows:",
                result.Unverified.Count(), result.RuleStatusList.Count);
        else
            logger.Log(LogLevel.Information, "All {0} rules passed validation.", result.RuleStatusList.Count);

        foreach (var unverified in result.Unverified)
        {
            logger.Log(LogLevel.Error, "Failed to validate {0}", unverified.RulesId);
            foreach (var error in unverified.Errors) logger.Log(LogLevel.Error, error);

            foreach (var oatError in unverified.OatIssues) logger.Log(LogLevel.Error, oatError.Description);
        }

        logger.Log(LogLevel.Information, "{0} of {1} rules have positive self tests.",
            result.RuleStatusList.Count(x => x.HasPositiveSelfTests), result.RuleStatusList.Count);
        logger.Log(LogLevel.Information, "{0} of {1} rules have negative self tests.",
            result.RuleStatusList.Count(x => x.HasNegativeSelfTests), result.RuleStatusList.Count);

        foreach (var rule in result.RuleStatusList.Where(x => !x.HasPositiveSelfTests))
            logger.Log(LogLevel.Warning, "Rule {0} does not have any positive test cases", rule.RulesId);
        foreach (var rule in result.RuleStatusList.Where(x => !x.HasNegativeSelfTests))
            logger.Log(LogLevel.Warning, "Rule {0} does not have any negative test cases", rule.RulesId);

        Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
        Assert.AreNotEqual(0, result.RuleStatusList.Count);
    }

    [TestMethod]
    public void VerifyNonZeroDefaultRules()
    {
        var set = RuleSetUtils.GetDefaultRuleSet();
        Assert.IsTrue(set.Any());

        RulesVerifier verifier = new(new RulesVerifierOptions());
        var result = verifier.Verify(set);

        Assert.IsTrue(result.Verified);
        Assert.AreNotEqual(0, result.RuleStatuses.Count);
        Assert.AreNotEqual(0, result.CompiledRuleSet.GetAppInspectorRules().Count());
    }
}