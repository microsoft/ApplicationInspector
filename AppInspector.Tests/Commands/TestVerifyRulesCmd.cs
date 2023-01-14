using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CommandLine;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.Commands;

// TODO: This does not intentionally try to make the OAT rule maker fail
// The OAT rules are being validated but there aren't test cases that intentionally try to break it.
[TestClass]
[ExcludeFromCodeCoverage]
public class TestVerifyRulesCmd
{
    // FileRegexes if specified must be valid
    private readonly string _invalidFileRegexes = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS_INVALID_REGEX"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""applies_to_file_regex"": [ ""$(^""],
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
}
]";

    // Rules are a List<Rule> so they must be contained in []
    private readonly string _invalidJsonValidRule = @"
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS_INVALID_JSON"",
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
}";

    // Languages if specified must be known
    private readonly string _knownLanguages = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS_INVALID_LANGAUGE"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""applies_to"": [ ""malboge""],
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
}
]";

    // MustMatch if specified must be matched
    private readonly string _mustMatchRule = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS_MUST_MATCH"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""applies_to"": [ ""csharp""],
    ""severity"": ""Important"",
    ""patterns"": [
      {
        ""confidence"": ""Medium"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""windows"",
        ""type"": ""String""
      }
    ],
    ""must-match"" : [ ""windows 2000""]
}
]";

    // MustMatch if specified must not fail to match
    private readonly string _mustMatchRuleFail = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS_MUST_MATCH_FAIL"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""applies_to"": [ ""csharp""],
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
    ],
    ""must-match"" : [ ""wimdoos""]
}
]";

    // MustNotMatch if specified must not be matched
    private readonly string _mustNotMatchRule = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS_MUST_NOT_MATCH"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""applies_to"": [ ""csharp""],
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
    ],
    ""must-not-match"" : [ ""linux""]
}
]";

    // MustNotMatch if specified must not fail to not be matched
    private readonly string _mustNotMatchRuleFail = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS_MUST_NOT_MATCH_FAIL"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""applies_to"": [ ""csharp""],
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
    ],
    ""must-not-match"" : [ ""windows""]
}
]";

    // Two rules may not have the same id
    private readonly string _sameId = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS_SAME_ID"",
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
    ""id"": ""AI_TEST_WINDOWS_SAME_ID"",
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

    // Rules must contain an id
    private readonly string _validJsonInvalidRuleNoId = @"[{
    ""name"": ""Platform: Microsoft Windows"",
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
}]";

    private readonly string _validRules = @"[
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
}
]";

    private ILoggerFactory _factory = new NullLoggerFactory();
    private readonly LogOptions _logOptions = new();
    private string _validRulesPath = string.Empty;

    [TestInitialize]
    public void InitOutput()
    {
        _factory = _logOptions.GetLoggerFactory();
        Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
        _validRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "TestRules.json");
        File.WriteAllText(_validRulesPath, _validRules);
    }

    [ClassCleanup]
    public static void CleanUp()
    {
        try
        {
            Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
        }
    }

    /// <summary>
    ///     Ensure an exception is thrown if you don't specify any rules to verify
    /// </summary>
    [TestMethod]
    public void NoDefaultNoCustomRules()
    {
        Assert.ThrowsException<OpException>(() => new VerifyRulesCommand(new VerifyRulesOptions()));
    }

    [TestMethod]
    public void CustomRules()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _validRulesPath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();

        Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
    }

    [TestMethod]
    public void UnclosedJson()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(path, _invalidJsonValidRule);
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = path
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        File.Delete(path);
        Assert.AreEqual(VerifyRulesResult.ExitCode.CriticalError, result.ResultCode);
    }

    [TestMethod]
    public void NullId()
    {
        var set = new RuleSet();
        set.AddString(_validJsonInvalidRuleNoId, "NoIdTest");
        RulesVerifierOptions options = new()
        {
            LoggerFactory = _factory
        };
        var rulesVerifier = new RulesVerifier(options);
        Assert.IsFalse(rulesVerifier.Verify(set).Verified);
    }

    [TestMethod]
    public void DuplicateId()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(path, _sameId);
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = path
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
        File.Delete(path);
    }

    [TestMethod]
    public void DuplicateIdCheckDisabled()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(path, _sameId);
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = path,
            DisableRequireUniqueIds = true
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
        File.Delete(path);
    }

    [TestMethod]
    public void InvalidRegex()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(path, _invalidFileRegexes);
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = path
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        File.Delete(path);
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }

    [TestMethod]
    public void UnknownLanguage()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(path, _knownLanguages);
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = path
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        File.Delete(path);
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }

    [TestMethod]
    public void MustMatch()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(path, _mustMatchRule);
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = path
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        File.Delete(path);
        Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
    }

    [TestMethod]
    public void MustMatchDetectIncorrect()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(path, _mustMatchRuleFail);
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = path
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        File.Delete(path);
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }

    [TestMethod]
    public void MustNotMatch()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(path, _mustNotMatchRule);
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = path
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        File.Delete(path);
        Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
    }

    [TestMethod]
    public void MustNotMatchDetectIncorrect()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(path, _mustNotMatchRuleFail);
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = path
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        File.Delete(path);
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }
}