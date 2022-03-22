namespace UnitTest.Commands.Tests_DefaultRules
{
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for the default set of rules which are embedded in the executable.
    /// </summary>
    [TestClass]
    public class LogOptionsTests
    {
        // This test ensures that the rules that are bundled with Application Inspector are valid.
        [TestMethod]
        public void VerifyDefaultRules()
        {
            VerifyRulesOptions options = new()
            {
                VerifyDefaultRules = true
            };

            VerifyRulesCommand command = new(options);
            VerifyRulesResult result = command.GetResult();

            Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
        }
    }
}
