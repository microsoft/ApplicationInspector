using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace AppInspector.Tests.DefaultRules
{
    /// <summary>
    /// Tests for the default set of rules which are embedded in the executable.
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestDefaultRules
    {
        // This test ensures that the rules that are bundled with Application Inspector are valid.
        [TestMethod]
        public void VerifyDefaultRules()
        {
            VerifyRulesOptions options = new()
            {
                VerifyDefaultRules = true,
            };
            var loggerFactory = new LogOptions() {ConsoleVerbosityLevel = LogEventLevel.Verbose}.GetLoggerFactory();
            VerifyRulesCommand command = new(options, loggerFactory);
            VerifyRulesResult result = command.GetResult();
            foreach (var unverified in result.Unverified)
            {
                Console.WriteLine("Failed to validate {0}",unverified.RulesId);
                foreach (var error in unverified.Errors)
                {
                    Console.WriteLine(error);
                }

                foreach (var oatError in unverified.OatIssues)
                {
                    Console.WriteLine(oatError.Description);
                }
            }
            Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
            Assert.AreNotEqual(0, result.RuleStatusList.Count);
        }

        [TestMethod]
        public void VerifyNonZeroDefaultRules()
        {
            RuleSet set = RuleSetUtils.GetDefaultRuleSet();
            Assert.IsTrue(set.Any());

            RulesVerifier verifier = new(new RulesVerifierOptions());
            RulesVerifierResult result = verifier.Verify(set);
            
            Assert.IsTrue(result.Verified);
            Assert.AreNotEqual(0, result.RuleStatuses.Count);
            Assert.AreNotEqual(0, result.CompiledRuleSet.GetAppInspectorRules().Count());
        }
    }
}
