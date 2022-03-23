namespace ApplicationInspector.Unitprocess.Commands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestPackRulesCmd
    {
        [TestInitialize]
        public void InitOutput()
        {
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
        }

        [TestCleanup]
        public void CleanUp()
        {
            try
            {
                Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
            }
            catch
            {
            }
        }

        [TestMethod]
        public void NoCustomNoEmbeddedRules()
        {
            Assert.ThrowsException<OpException>(() => new PackRulesCommand(new()));
        }

        [TestMethod]
        public void PackEmbeddedRules()
        {
            PackRulesOptions options = new() { PackEmbeddedRules = true };
            PackRulesCommand command = new(options);
            PackRulesResult result = command.GetResult();
            Assert.AreEqual(PackRulesResult.ExitCode.Success, result.ResultCode);
        }
    }
}