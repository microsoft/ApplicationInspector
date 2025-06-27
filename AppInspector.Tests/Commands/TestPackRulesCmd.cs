using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Xunit;

namespace AppInspector.Tests.Commands;

[ExcludeFromCodeCoverage]
public class TestPackRulesCmd
{
    [Fact]
    public void NoCustomNoEmbeddedRules()
    {
        Assert.Throws<OpException>(() => new PackRulesCommand(new PackRulesOptions()));
    }

    [Fact]
    public void PackEmbeddedRules()
    {
        PackRulesOptions options = new() { PackEmbeddedRules = true };
        PackRulesCommand command = new(options);
        var result = command.GetResult();
        Assert.Equal(PackRulesResult.ExitCode.Success, result.ResultCode);
    }
}