using System;
using System.Collections.Generic;
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
/// Special attention is paid to duplicate value scenarios to ensure accurate boundary mapping
/// for complex XML content including:
/// 1. Multiple XPath matches with identical content values
/// 2. Element inner text and attribute values with the same content
/// 3. Namespace handling in XPath queries
/// </summary>
public class XPathBoundaryTests
{
    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();



    [Fact]
    public void Boundary_Element_ExactMatch()
    {
        var xml = "<?xml version=\"1.0\"?>\n<root>\n    <item>ExactValue</item>\n</root>";
        var processor = RuleTestHelpers.BuildRuleAndProcessor("BOUND_ELEM", "ExactValue", "string", "//item");

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
        var processor = RuleTestHelpers.BuildRuleAndProcessor("BOUND_ATTR", "AttrValue", "string", "//item/@attr");

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
        var processor = RuleTestHelpers.BuildRuleAndProcessor("BOUND_SUB", "TargetValue", "substring", "//data");

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

        var processor = RuleTestHelpers.BuildRuleAndProcessor("BOUND_LAST", "Val49", "string", "//item[@id='49']");

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
        var processor = RuleTestHelpers.BuildRuleAndProcessor("BOUND_DUP_ELEM", "SameValue", "string", "//item");

        Assert.True(_languages.FromFileNameOut("test.xml", out var info));
        var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info).ToList();

        // Expect one match per <item> element (3 total)
        Assert.Equal(3, matches.Count);
        foreach (var m in matches)
        {
            var extracted = xml.Substring(m.Boundary.Index, m.Boundary.Length);
            Assert.Equal("SameValue", extracted);
        }

        // Verify all boundaries are distinct for different element positions
        var boundaryStarts = matches.Select(m => m.Boundary.Index).OrderBy(i => i).ToArray();
        var distinctBoundaries = boundaryStarts.Distinct().Count();
        
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
        var processor = RuleTestHelpers.CreateProcessor(rs);

        Assert.True(_languages.FromFileNameOut("test.xml", out var info));
        var matches = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info).ToList();

        // Expect 4 matches: 3 attributes + element text
        Assert.Equal(4, matches.Count);
        
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

        // Verify all boundaries are distinct for different XPath matches  
        var allBoundaries = matches.Select(m => m.Boundary.Index).ToArray();
        var distinctBoundaries = allBoundaries.Distinct().Count();
        
        Assert.True(distinctBoundaries == allBoundaries.Length, "All boundaries should be distinct for different XPath matches");
    }
}