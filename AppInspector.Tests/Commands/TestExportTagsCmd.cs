using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.Commands;

[TestClass]
[ExcludeFromCodeCoverage]
public class TestExportTagsCmd
{
    private ILoggerFactory factory = new NullLoggerFactory();

    private readonly LogOptions logOptions = new();
    private string testRulesPath = string.Empty;

    [TestInitialize]
    public void InitOutput()
    {
        factory = logOptions.GetLoggerFactory();
        testRulesPath = Path.Combine("TestData","TestExportTagsCmd","Rules", "TestRules.json");
    }

    [TestMethod]
    public void ExportCustom()
    {
        ExportTagsOptions options = new()
        {
            IgnoreDefaultRules = true,
            CustomRulesPath = testRulesPath
        };
        ExportTagsCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.IsTrue(result.TagsList.Contains("Test.Tags.Linux"));
        Assert.IsTrue(result.TagsList.Contains("Test.Tags.Windows"));
        Assert.AreEqual(2, result.TagsList.Count);
        Assert.AreEqual(ExportTagsResult.ExitCode.Success, result.ResultCode);
    }

    [TestMethod]
    public void ExportDefault()
    {
        ExportTagsOptions options = new()
        {
            IgnoreDefaultRules = false
        };
        ExportTagsCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.AreEqual(ExportTagsResult.ExitCode.Success, result.ResultCode);
    }

    [TestMethod]
    public void NoDefaultNoCustomRules()
    {
        ExportTagsOptions options = new()
        {
            IgnoreDefaultRules = true
        };
        Assert.ThrowsException<OpException>(() => new ExportTagsCommand(options));
    }
}