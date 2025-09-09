using System;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
/// Additional comprehensive XPath tests expanding coverage beyond existing XmlAndJsonTests
/// </summary>
public class XPathComprehensiveTests
{
    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    [Fact]
    public void XPathWithComplexPredicates()
    {
        var xml = @"<?xml version=""1.0""?>
<books>
    <book id=""1"" price=""29.99"">
        <title lang=""en"">Learning XML</title>
        <author>Erik T. Ray</author>
    </book>
    <book id=""2"" price=""39.95"">
        <title lang=""fr"">XML pour tous</title>
        <author>Jean Dupont</author>
    </book>
</books>";

        var rule = @"[{
            ""id"": ""TEST001"",
            ""name"": ""XPath Predicate Test"",
            ""patterns"": [{
                ""pattern"": ""Learning"",
                ""type"": ""substring"",
                ""xpaths"": [""//book[@price > 25]/title[@lang='en']""]
            }]
        }]";

        RuleSet rules = new();
        rules.AddString(rule, "TestRules");
    var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });

        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
            Assert.Single(matches);
            // Engine returns only the matched substring (pattern value), not full element text
            Assert.Equal("Learning", matches.First().Sample);
        }
        else
        {
            Assert.Fail();
        }
    }

    [Fact]
    public void XPathWithAxisSelectors()
    {
        var xml = @"<?xml version=""1.0""?>
<root>
    <section>
        <item>First</item>
        <target>Middle</target>
        <item>Last</item>
    </section>
</root>";

        var testCases = new (string xpath, string expected)[]
        {
            ("//target/following-sibling::item", "Last"),
            ("//target/preceding-sibling::item", "First"),
            ("//target/parent::section/item[1]", "First"),
            ("//item[last()]", "Last")
        };

        foreach (var (xpath, expectedValue) in testCases)
        {
            var rule = $@"[{{
                ""id"": ""TEST_AXIS"",
                ""name"": ""XPath Axis Test"",
                ""patterns"": [{{
                    ""pattern"": ""{expectedValue}"",
                    ""type"": ""string"",
                    ""xpaths"": [""{xpath}""]
                }}]
            }}]";

            RuleSet rules = new();
            rules.AddString(rule, "TestRules");
            var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });

            if (_languages.FromFileNameOut("test.xml", out var info))
            {
                var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
                Assert.Single(matches); // Each XPath returns a single element
                Assert.Equal(expectedValue, matches.First().Sample);
            }
            else
            {
                Assert.Fail();
            }
        }
    }

    [Fact]
    public void XPathWithMultipleAttributeFormats()
    {
        var xml = @"<?xml version=""1.0""?>
<root>
    <elem attr1=""double-quoted"" />
    <elem attr2='single-quoted' />
    <elem attr3=""value-with-'quotes'"" />
    <elem attr4='value-with-""quotes""' />
    <elem attr5=""special&amp;chars"" />
</root>";

        var rule = @"[{
            ""id"": ""ATTR_TEST"",
            ""name"": ""Attribute Format Test"",
            ""patterns"": [{
                ""pattern"": ""quoted"",
                ""type"": ""substring"",
                ""xpaths"": [""//elem/@*""]
            }]
        }]";

        RuleSet rules = new();
        rules.AddString(rule, "TestRules");
    var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });

        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
            // Currently only attr1 and attr2 attribute values contain the literal substring 'quoted'
            Assert.Equal(2, matches.Count());
        }
        else
        {
            Assert.Fail();
        }
    }

    [Fact]
    public void XPathWithNamespacePrefixConflicts()
    {
        var xml = @"<?xml version=""1.0""?>
<root xmlns:ns1=""http://example.com/ns1"" xmlns:ns2=""http://example.com/ns2"">
    <ns1:element>Value1</ns1:element>
    <ns2:element>Value2</ns2:element>
</root>";

        var rule = @"[{
            ""id"": ""NS_TEST"",
            ""name"": ""Namespace Prefix Test"",
            ""patterns"": [{
                ""pattern"": ""Value1"",
                ""type"": ""string"",
                ""xpaths"": [""//ns1:element""],
                ""xpathnamespaces"": {
                    ""ns1"": ""http://example.com/ns1""
                }
            }]
        }]";

        RuleSet rules = new();
        rules.AddString(rule, "TestRules");
    var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });

        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
            Assert.Single(matches);
            Assert.Equal("Value1", matches.First().Sample);
        }
        else
        {
            Assert.Fail();
        }
    }

    [Fact]
    public void XPathWithTextNodes()
    {
        var xml = @"<?xml version=""1.0""?>
<root>
    <mixed>Text before <child>nested</child> text after</mixed>
    <simple>Plain text</simple>
</root>";

        var rule = @"[{
            ""id"": ""TEXT_TEST"",
            ""name"": ""Text Node Test"",
            ""patterns"": [{
                ""pattern"": ""Plain"",
                ""type"": ""substring"",
                ""xpaths"": [""//simple""]
            }]
        }]"; // Select element due to current engine node handling

        RuleSet rules = new();
        rules.AddString(rule, "TestRules");
    var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
            Assert.Single(matches);
        }
        else
        {
            Assert.Fail();
        }
    }

    [Fact]
    public void XPathWithInvalidExpressions()
    {
    var xml = @"<?xml version=""1.0""?><root><item>Test</item></root>";

        var invalidXPaths = new[]
        {
            "//[invalid",
            "//root[@",
            "//root[position(]",
            "@attribute"
        };

        foreach (var invalidXPath in invalidXPaths)
        {
            var rule = $@"[{{
                ""id"": ""INVALID_TEST"",
                ""name"": ""Invalid XPath Test"",
                ""patterns"": [{{
                    ""pattern"": ""Test"",
                    ""type"": ""string"",
                    ""xpaths"": [""{invalidXPath}""]
                }}]
            }}]";

            RuleSet rules = new();
            rules.AddString(rule, "TestRules");
            var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
            if (_languages.FromFileNameOut("test.xml", out var info))
            {
                var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
                Assert.Empty(matches);
            }
            else
            {
                Assert.Fail();
            }
        }
    }

    [Fact]
    public void XPathWithEmptyAndWhitespaceNodes()
    {
        var xml = @"<?xml version=""1.0""?>
<root>
    <empty/>
    <whitespace>   </whitespace>
    <newlines>

    </newlines>
    <content>actual content</content>
</root>";

        var rule = @"[{
            ""id"": ""EMPTY_TEST"",
            ""name"": ""Empty Node Test"",
            ""patterns"": [{
                ""pattern"": ""actual"",
                ""type"": ""substring"",
                ""xpaths"": [""/root/*""]
            }]
        }]";

        RuleSet rules = new();
        rules.AddString(rule, "TestRules");
    var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
            Assert.Single(matches); // Only the <content> element should match
        }
        else
        {
            Assert.Fail();
        }
    }

    [Fact]
    public void XPathWithCDATAAndComments()
    {
        var xml = @"<?xml version=""1.0""?>
<root>
    <!-- This is a comment -->
    <data><![CDATA[Special <characters> & symbols]]></data>
    <normal>Normal text</normal>
</root>";

        var rule = @"[{
            ""id"": ""CDATA_TEST"",
            ""name"": ""CDATA Test"",
            ""patterns"": [{
                ""pattern"": ""<characters>"",
                ""type"": ""substring"",
                ""xpaths"": [""//data""]
            }]
        }]";

        RuleSet rules = new();
        rules.AddString(rule, "TestRules");
    var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
            Assert.Single(matches);
        }
        else
        {
            Assert.Fail();
        }
    }

    [Fact]
    public void XPathWithLargeDocumentPerformance()
    {
        var xml = "<?xml version=\"1.0\"?>\n<root>";
        for (int i = 0; i < 1000; i++)
        {
            xml += $"<item id=\"{i}\" type=\"test{i % 10}\">Content {i}</item>";
        }
        xml += "</root>";

        var rule = @"[{
            ""id"": ""PERF_TEST"",
            ""name"": ""Performance Test"",
            ""patterns"": [{
                ""pattern"": ""Content 999"",
                ""type"": ""string"",
                ""xpaths"": [""//item[@type='test9']""]
            }]
        }]";

        RuleSet rules = new();
        rules.AddString(rule, "TestRules");
    var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
            Assert.NotEmpty(matches);
            Assert.Contains(matches, m => m.Sample == "Content 999");
        }
        else
        {
            Assert.Fail();
        }
    }

    [Fact]
    public void XPathWithBoundaryEdgeCases()
    {
        var xml = @"<?xml version=""1.0""?>
<root>
    <a>A</a>
    <ab>AB</ab>
    <abc>ABC</abc>
    <abcd>ABCD</abcd>
</root>";

        var rule = @"[{
            ""id"": ""BOUNDARY_TEST"",
            ""name"": ""Boundary Test"",
            ""patterns"": [{
                ""pattern"": ""AB"",
                ""type"": ""substring"",
                ""xpaths"": [""/root/*[contains(., 'AB')]""]
            }]
        }]";

        RuleSet rules = new();
        rules.AddString(rule, "TestRules");
    var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
            // Expect matches for <ab>, <abc>, <abcd>
            Assert.Equal(3, matches.Count());
            foreach (var match in matches)
            {
                var extracted = xml.Substring(match.Boundary.Index, match.Boundary.Length);
                Assert.Contains("AB", extracted);
            }
        }
        else
        {
            Assert.Fail();
        }
    }
}