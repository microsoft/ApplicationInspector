using System.Diagnostics.CodeAnalysis;
using Microsoft.ApplicationInspector.CLI;
using Microsoft.ApplicationInspector.Commands;
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

            Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
        }
    }
}
