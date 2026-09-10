using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Microsoft.ApplicationInspector.CLI;
using Xunit;

namespace AppInspector.Tests.CLI;

/// <summary>
///     Verifies that the symlink command line option is wired to the commands that accept a source path.
/// </summary>
[ExcludeFromCodeCoverage]
public class TestFollowSymlinksOption
{
    [Fact]
    public void AnalyzeDefaultsToSkippingSymlinks()
    {
        var options = Parse<CLIAnalyzeCmdOptions>("analyze", "-s", "sourcePath");

        Assert.False(options.FollowSymlinks);
    }

    [Fact]
    public void AnalyzeAcceptsFollowSymlinks()
    {
        var options = Parse<CLIAnalyzeCmdOptions>("analyze", "-s", "sourcePath", "--follow-symlinks");

        Assert.True(options.FollowSymlinks);
    }

    [Fact]
    public void TagDiffDefaultsToSkippingSymlinks()
    {
        var options = Parse<CLITagDiffCmdOptions>("tagdiff", "--src1", "one", "--src2", "two");

        Assert.False(options.FollowSymlinks);
    }

    [Fact]
    public void TagDiffAcceptsFollowSymlinks()
    {
        var options = Parse<CLITagDiffCmdOptions>("tagdiff", "--src1", "one", "--src2", "two", "--follow-symlinks");

        Assert.True(options.FollowSymlinks);
    }

    private static T Parse<T>(params string[] args) where T : class
    {
        T? parsed = null;
        Parser.Default.ParseArguments<CLIAnalyzeCmdOptions, CLITagDiffCmdOptions>(args)
            .WithParsed<T>(options => parsed = options);

        Assert.NotNull(parsed);
        return parsed!;
    }
}
