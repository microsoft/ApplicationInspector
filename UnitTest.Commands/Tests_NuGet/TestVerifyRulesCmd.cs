namespace ApplicationInspector.Unitprocess.Commands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.CLI;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;

    [TestClass]
    public class TestVerifyRulesCmd
    {
        private string validRulesPath = string.Empty;
        private LogOptions logOptions = new();
        private ILoggerFactory factory = new NullLoggerFactory();
        [TestInitialize]
        public void InitOutput()
        {
            factory = logOptions.GetLoggerFactory();
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
            validRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "TestRules.json");
            File.WriteAllText(validRulesPath, validRules);
        }

        [TestCleanup]
        public void CleanUp()
        {
            Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
        }

        string validRules = @"[
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
        string invalidJsonValidRule = @"
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
        string validJsonInvalidRule_NoId = @"[{
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
        string sameId = @"[
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
        string knownLanguages = @"[
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
        string invalidFileRegexes = @"[
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
                CustomRulesPath = validRulesPath,
            };

            VerifyRulesCommand command = new(options, factory);
            VerifyRulesResult result = command.GetResult();

            Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
        }

        [TestMethod]
        public void UnclosedJson()
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, invalidJsonValidRule);
            VerifyRulesOptions options = new()
            {
                CustomRulesPath = path,
            };

            VerifyRulesCommand command = new(options, factory);
            VerifyRulesResult result = command.GetResult();
            File.Delete(path);
            Assert.AreEqual(VerifyRulesResult.ExitCode.CriticalError, result.ResultCode);
        }
        
    }
}