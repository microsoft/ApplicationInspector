using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.Commands;

[TestClass]
[ExcludeFromCodeCoverage]
public class TestPackRulesCmd
{
    [TestMethod]
    public void NoCustomNoEmbeddedRules()
    {
        Assert.ThrowsException<OpException>(() => new PackRulesCommand(new PackRulesOptions()));
    }

    [TestMethod]
    public void PackEmbeddedRules()
    {
        PackRulesOptions options = new() { PackEmbeddedRules = true };
        PackRulesCommand command = new(options);
        var result = command.GetResult();
        Assert.AreEqual(PackRulesResult.ExitCode.Success, result.ResultCode);
    }
}