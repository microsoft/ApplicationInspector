namespace ApplicationInspector.Unitprocess.Language
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.RulesEngine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;

    [TestClass]
    public class LanguagesTests
    {
        [TestMethod]
        public void DetectCustomLanguage()
        {
            var languages = new Languages(Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"languageTestFiles\comments_z_only.json"), Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"languageTestFiles\languages_z_only.json"));
            Assert.IsTrue(languages.FromFileNameOut("afilename.z", out var language));
            Assert.AreEqual("z",language.Name);
            Assert.IsFalse(languages.FromFileNameOut("afilename.c", out var language2));
        }
    }  
}
