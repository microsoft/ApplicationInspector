namespace ApplicationInspector.Unitprocess.Language
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.RulesEngine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class LanguagesTests
    {
        readonly string comments_z = @"
[
  {
    ""language"": [
      ""z""
    ],
    ""inline"": ""//"",
    ""prefix"": ""/*"",
    ""suffix"": ""*/""
  }
]";
        readonly string languages_z = @"
        [
  {
    ""name"": ""z"",
    ""extensions"": [ "".z"", "".xw"" ],
    ""type"": ""code""
  }
]";

        private string testLanguagesPath = string.Empty;
        private string testCommentsPath = string.Empty;

        [TestInitialize]
        public void InitOutput()
        {
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
            testLanguagesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "test_languages.json");
            testCommentsPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "test_comments.json");
            File.WriteAllText(testLanguagesPath, languages_z);
            File.WriteAllText(testCommentsPath, comments_z);
        }

        [TestCleanup]
        public void CleanUp()
        {
            Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
        }
        [TestMethod]
        public void DetectCustomLanguage()
        {
            var languages = new Languages(null, testCommentsPath, testLanguagesPath);
            Assert.IsTrue(languages.FromFileNameOut("afilename.z", out var language));
            Assert.AreEqual("z", language.Name);
            Assert.IsFalse(languages.FromFileNameOut("afilename.c", out var _));
        }
    }
}