using System;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
/// Tests focused specifically on validating Boundary.Index/Length correctness for XPath-driven matches.
/// These are intended to catch off-by-one or misaligned index mapping regressions.
/// 
/// Special attention is paid to duplicate value scenarios that can cause boundary mapping ambiguities.
/// 
/// NOTE: Some tests currently document known boundary mapping limitations where duplicate values 
/// in XML content can result in:
/// 1. Multiple XPath matches mapping to the same boundary position  
/// 2. Element inner text falling back to global search and colliding with attribute boundaries
/// 
/// These tests will pass once the boundary disambiguation logic is improved.
/// </summary>
public class XPathBoundaryTests
{
    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    private static RuleSet BuildRule(string id, string pattern, string type, string xpath, string? namespacesJson = null)
    {
        var nsFragment = string.IsNullOrWhiteSpace(namespacesJson) ? string.Empty : ",\n                \"xpathnamespaces\": " + namespacesJson;
        var ruleJson = "[{" +
                       "\n    \"id\": \"" + id + "\"," +
                       "\n    \"name\": \"" + id + " Test\"," +
                       "\n    \"patterns\": [{" +
                       "\n        \"pattern\": \"" + pattern + "\"," +
                       "\n        \"type\": \"" + type + "\"," +
                       "\n        \"xpaths\": [\"" + xpath + "\"]" +
                       nsFragment +
                       "\n    }]" +
                       "\n}]";
        RuleSet rs = new();
        rs.AddString(ruleJson, "TestRules");
        return rs;
    }

    [Fact]
    public void Boundary_Element_ExactMatch()
    {
        var xml = "<?xml version=\"1.0\"?>\n<root>\n    <item>ExactValue</item>\n</root>";
        var rules = BuildRule("BOUND_ELEM", "ExactValue", "string", "//item");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });

        Assert.True(_languages.FromFileNameOut("test.xml", out var info));
        var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
        var match = Assert.Single(matches);

        Assert.Equal(match.Sample.Length, match.Boundary.Length);
        var extracted = xml.Substring(match.Boundary.Index, match.Boundary.Length);
        Assert.Equal(match.Sample, extracted);
        Assert.Equal('E', extracted[0]);
        Assert.Equal('e', extracted[^1]);
    }

    [Fact]
    public void Boundary_Attribute_ExactMatch()
    {
        var xml = "<?xml version=\"1.0\"?>\n<root>\n  <item attr=\"AttrValue\">X</item>\n</root>";
        var rules = BuildRule("BOUND_ATTR", "AttrValue", "string", "//item/@attr");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });

        Assert.True(_languages.FromFileNameOut("test.xml", out var info));
        var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
        var match = Assert.Single(matches);

        Assert.Equal(match.Sample.Length, match.Boundary.Length);
        var extracted = xml.Substring(match.Boundary.Index, match.Boundary.Length);
        Assert.Equal("AttrValue", extracted);
        Assert.Equal(match.Sample, extracted);

        var attrDecl = "attr=\"AttrValue\"";
        var attrStart = xml.IndexOf(attrDecl, StringComparison.Ordinal);
        Assert.InRange(match.Boundary.Index, attrStart + 6, attrStart + attrDecl.Length - 1);
    }

    [Fact]
    public void Boundary_Element_SubstringPattern()
    {
        var xml = "<?xml version=\"1.0\"?>\n<root>\n  <data>Prefix-TargetValue-Suffix</data>\n</root>";
        var rules = BuildRule("BOUND_SUB", "TargetValue", "substring", "//data");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });

        Assert.True(_languages.FromFileNameOut("test.xml", out var info));
        var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
        var match = Assert.Single(matches);

        var extracted = xml.Substring(match.Boundary.Index, match.Boundary.Length);
        Assert.Contains(match.Sample, extracted);
        Assert.Equal(xml[match.Boundary.Index], extracted[0]);
        Assert.Equal(xml[match.Boundary.Index + match.Boundary.Length - 1], extracted[^1]);
    }

    [Fact]
    public void Boundary_LastElement_InLargeDocument()
    {
        var xml = "<?xml version=\"1.0\"?>\n<root>";
        for (int i = 0; i < 50; i++)
        {
            xml += $"<item id=\"{i}\">Val{i}</item>\n";
        }
        xml += "</root>";

        var rules = BuildRule("BOUND_LAST", "Val49", "string", "//item[@id='49']");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });

        Assert.True(_languages.FromFileNameOut("test.xml", out var info));
        var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
        var match = Assert.Single(matches);
        var extracted = xml.Substring(match.Boundary.Index, match.Boundary.Length);
        Assert.Equal("Val49", extracted);
        Assert.InRange(match.Boundary.Index, 0, xml.Length - extracted.Length);
    }

    [Fact]
    public void Boundaries_DuplicateElementText_AllDistinct()
    {
        var xml = "<?xml version=\"1.0\"?>\n<root>\n  <item>SameValue</item>\n  <item>SameValue</item>\n  <item>SameValue</item>\n</root>";
        var rules = BuildRule("BOUND_DUP_ELEM", "SameValue", "string", "//item");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });

        Assert.True(_languages.FromFileNameOut("test.xml", out var info));
        var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info).ToList();

        // Expect one match per <item> element (3 total)
        Assert.Equal(3, matches.Count);
        foreach (var m in matches)
        {
            var extracted = xml.Substring(m.Boundary.Index, m.Boundary.Length);
            Assert.Equal("SameValue", extracted);
        }

        // Test for duplicate boundary issue: 
        // KNOWN LIMITATION: Some elements with identical inner text may map to the same boundary
        // This test documents the current behavior and will pass when the issue is fixed
        var boundaryStarts = matches.Select(m => m.Boundary.Index).OrderBy(i => i).ToArray();
        var distinctBoundaries = boundaryStarts.Distinct().Count();
        
        if (distinctBoundaries < boundaryStarts.Length)
        {
            // Document the known issue for future reference
            var duplicates = boundaryStarts.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
            Assert.Fail($"KNOWN BOUNDARY MAPPING ISSUE: Found {boundaryStarts.Length - distinctBoundaries} duplicate boundaries at positions: [{string.Join(", ", duplicates)}]. " +
                              $"All boundaries: [{string.Join(", ", boundaryStarts)}]. " +
                              $"This test documents the need for improved duplicate value boundary disambiguation.");
        }
        
        // This assertion will pass once the boundary logic is improved
        Assert.True(distinctBoundaries == boundaryStarts.Length, "All boundaries should be distinct for different element positions");
    }

    [Fact]
    public void Boundaries_DuplicateAttributeValues_SameLine()
    {
        // Multiple attributes with identical values on the same line plus element inner text sharing the value
        var xml = "<?xml version=\"1.0\"?>\n<root>\n  <item a=\"DupVal\" b=\"DupVal\" c=\"DupVal\">DupVal</item>\n</root>";

        // Build a rule set with separate rules for each attribute and one for the element text
        var rulesJson = "[" +
                        "{\n  \"id\": \"ATTR_A\", \n  \"name\": \"ATTR_A\", \n  \"patterns\": [{ \n    \"pattern\": \"DupVal\", \n    \"type\": \"string\", \n    \"xpaths\": [\"//item/@a\"] \n  }]\n}," +
                        "{\n  \"id\": \"ATTR_B\", \n  \"name\": \"ATTR_B\", \n  \"patterns\": [{ \n    \"pattern\": \"DupVal\", \n    \"type\": \"string\", \n    \"xpaths\": [\"//item/@b\"] \n  }]\n}," +
                        "{\n  \"id\": \"ATTR_C\", \n  \"name\": \"ATTR_C\", \n  \"patterns\": [{ \n    \"pattern\": \"DupVal\", \n    \"type\": \"string\", \n    \"xpaths\": [\"//item/@c\"] \n  }]\n}," +
                        "{\n  \"id\": \"ELEM_VAL\", \n  \"name\": \"ELEM_VAL\", \n  \"patterns\": [{ \n    \"pattern\": \"DupVal\", \n    \"type\": \"string\", \n    \"xpaths\": [\"//item\"] \n  }]\n}" +
                        "]";

        RuleSet rs = new();
        rs.AddString(rulesJson, "TestRules");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rs, new Microsoft.ApplicationInspector.RulesEngine.RuleProcessorOptions { AllowAllTagsInBuildFiles = true });

        Assert.True(_languages.FromFileNameOut("test.xml", out var info));
        var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info).ToList();

        // Expect 4 matches: 3 attributes + element text
        Assert.Equal(4, matches.Count);

        // Remove debug output and fix the test properly by accepting that the boundary logic 
        // currently has this limitation and focusing on demonstrating the issue exists
        
        // Let's check the actual boundaries to understand the current behavior 
        var attrAIndex = xml.IndexOf("a=\"DupVal\"") + "a=\"".Length;  // Should be around 44
        var attrBIndex = xml.IndexOf("b=\"DupVal\"") + "b=\"".Length;  // Should be around 55  
        var attrCIndex = xml.IndexOf("c=\"DupVal\"") + "c=\"".Length;  // Should be around 66
        var elemIndex = xml.IndexOf(">DupVal<") + 1;                    // Should be around 73
        
        Assert.True(matches.Count >= 3, $"Expected at least 3 matches but got {matches.Count}");
        
        // For now, let's verify that we get distinct boundaries for the attributes that work correctly
        var attrMatches = matches.Where(m => m.RuleId.StartsWith("ATTR_")).ToArray();
        if (attrMatches.Length >= 2)
        {
            var attrBoundaries = attrMatches.Select(m => m.Boundary.Index).ToArray();
            Assert.Equal(attrBoundaries.Distinct().Count(), attrBoundaries.Length);
        }

        // Verify each match extracts "DupVal"
        foreach (var m in matches)
        {
            var extracted = xml.Substring(m.Boundary.Index, m.Boundary.Length);
            Assert.Equal("DupVal", extracted);
        }

        // Test for boundary collision between element and attribute values
        // KNOWN LIMITATION: Element inner text may collide with attribute value boundary  
        var allBoundaries = matches.Select(m => m.Boundary.Index).ToArray();
        var distinctBoundaries = allBoundaries.Distinct().Count();
        
        if (distinctBoundaries < allBoundaries.Length)
        {
            var ruleIds = matches.Select(m => m.RuleId).ToArray();
            var boundaryInfo = matches.Select(m => $"{m.RuleId}@{m.Boundary.Index}").ToArray();
            Assert.Fail($"KNOWN BOUNDARY COLLISION ISSUE: Expected 4 distinct boundaries but found {distinctBoundaries}. " +
                       $"Boundary details: [{string.Join(", ", boundaryInfo)}]. " +
                       $"This indicates element XPath fallback is conflicting with attribute boundaries.");
        }
        
        // This assertion will pass once the element/attribute boundary separation is improved
        Assert.True(distinctBoundaries == allBoundaries.Length, "All boundaries should be distinct for different XPath matches");
    }
}