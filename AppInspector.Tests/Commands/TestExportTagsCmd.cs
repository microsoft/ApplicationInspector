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

    private readonly string findWindows = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""severity"": ""Important"",
    ""patterns"": [
      {
                ""confidence"": ""Medium"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""windows"",
        ""type"": ""String"",
      }
    ]
},
{
    ""name"": ""Platform: Linux"",
    ""id"": ""AI_TEST_LINUX"",
    ""description"": ""This rule checks for the string 'linux'"",
    ""tags"": [
      ""Test.Tags.Linux""
    ],
    ""severity"": ""Moderate"",
    ""patterns"": [
      {
                ""confidence"": ""High"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""linux"",
        ""type"": ""String"",
      }
    ]
},
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS_2"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""severity"": ""Important"",
    ""patterns"": [
      {
                ""confidence"": ""Medium"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""windows"",
        ""type"": ""String"",
      }
    ]
},
{
    ""name"": ""Platform: Linux"",
    ""id"": ""AI_TEST_LINUX_2"",
    ""description"": ""This rule checks for the string 'linux'"",
    ""tags"": [
      ""Test.Tags.Linux""
    ],
    ""severity"": ""Moderate"",
    ""patterns"": [
      {
                ""confidence"": ""High"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""linux"",
        ""type"": ""String"",
      }
    ]
}
]";

    private readonly LogOptions logOptions = new();
    private string testRulesPath = string.Empty;

    [TestInitialize]
    public void InitOutput()
    {
        factory = logOptions.GetLoggerFactory();
        Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
        testRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "TestRules.json");
        File.WriteAllText(testRulesPath, findWindows);
    }

    [ClassCleanup]
    public static void CleanUp()
    {
        Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
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