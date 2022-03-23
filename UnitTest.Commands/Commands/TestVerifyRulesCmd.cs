using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.ApplicationInspector.CLI;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.Commands
{
    // TODO: This does not intentionally try to make the OAT rule maker fail
    // The OAT rules are being validated but there aren't test cases that intentionally try to break it.
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestVerifyRulesCmd
    {
        private string _validRulesPath = string.Empty;
        private LogOptions _logOptions = new();
        private ILoggerFactory _factory = new NullLoggerFactory();
        [TestInitialize]
        public void InitOutput()
        {
            _factory = _logOptions.GetLoggerFactory();
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
            _validRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "TestRules.json");
            File.WriteAllText(_validRulesPath, _validRules);
        }

        [TestCleanup]
        public void CleanUp()
        {
            Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
        }

        readonly string _validRules = @"[
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
        // Rules are a List<Rule> so they must be contained in []
        readonly string _invalidJsonValidRule = @"
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
}";
        // Rules must contain an id
        readonly string _validJsonInvalidRuleNoId = @"[{
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
        // Two rules may not have the same id
        readonly string _sameId = @"[
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
    ""id"": ""AI_TEST_WINDOWS"",
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

        // Languages if specified must be known
        readonly string _knownLanguages = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS"",
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

        // FileRegexes if specified must be valid
        readonly string _invalidFileRegexes = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS"",
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
        /// <summary>
        /// Ensure an exception is thrown if you don't specify any rules to verify
        /// </summary>
        [TestMethod]
        public void NoDefaultNoCustomRules()
        {
            Assert.ThrowsException<OpException>(() => new VerifyRulesCommand(new()));
        }

        [TestMethod]
        public void CustomRules()
        {
            VerifyRulesOptions options = new()
            {
                CustomRulesPath = _validRulesPath,
            };

            VerifyRulesCommand command = new(options, _factory);
            VerifyRulesResult result = command.GetResult();

            Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
        }

        [TestMethod]
        public void UnclosedJson()
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, _invalidJsonValidRule);
            VerifyRulesOptions options = new()
            {
                CustomRulesPath = path,
            };

            VerifyRulesCommand command = new(options, _factory);
            VerifyRulesResult result = command.GetResult();
            File.Delete(path);
            Assert.AreEqual(VerifyRulesResult.ExitCode.CriticalError, result.ResultCode);
        }

        [TestMethod]
        public void NullId()
        {
            RuleSet set = new RuleSet();
            set.AddString(_validJsonInvalidRuleNoId, "NoIdTest");
            RulesVerifierOptions options = new()
            {
                FailFast = false,
                LoggerFactory = _factory
            };
            RulesVerifier rulesVerifier = new RulesVerifier(options);
            Assert.IsFalse(rulesVerifier.Verify(set).Verified);
        }

        [TestMethod]
        public void DuplicateId()
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, _sameId);
            VerifyRulesOptions options = new()
            {
                CustomRulesPath = path,
            };

            VerifyRulesCommand command = new(options, _factory);
            VerifyRulesResult result = command.GetResult();
            File.Delete(path);
            Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
        }

        [TestMethod]
        public void InvalidRegex()
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, _invalidFileRegexes);
            VerifyRulesOptions options = new()
            {
                CustomRulesPath = path,
            };

            VerifyRulesCommand command = new(options, _factory);
            VerifyRulesResult result = command.GetResult();
            File.Delete(path);
            Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
        }

        [TestMethod]
        public void UnknownLanguage()
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, _knownLanguages);
            VerifyRulesOptions options = new()
            {
                CustomRulesPath = path,
            };

            VerifyRulesCommand command = new(options, _factory);
            VerifyRulesResult result = command.GetResult();
            File.Delete(path);
            Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
        }
    }
}