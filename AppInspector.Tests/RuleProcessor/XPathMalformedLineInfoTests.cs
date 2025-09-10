using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using RuleLanguages = Microsoft.ApplicationInspector.RulesEngine.Languages;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
/// Tests to ensure defensive handling of malformed or out-of-range XML line info (line/column) does not throw
/// and still produces correct boundaries via fallback search logic.
/// </summary>
public class XPathMalformedLineInfoTests
{
    private readonly RuleLanguages _languages = new();

    private static object CreateLineInfoAnnotation(int line, int col)
    {
        var annotationType = typeof(XObject).Assembly.GetType("System.Xml.Linq.LineInfoAnnotation")
            ?? throw new InvalidOperationException("LineInfoAnnotation type not found.");
        var inst = Activator.CreateInstance(annotationType, line, col)
            ?? throw new InvalidOperationException("Failed to create LineInfoAnnotation instance.");
        return inst;
    }

    [Fact]
    public void XPath_With_ExcessivelyLargeLine_DoesNotThrow_AndFindsValue()
    {
        const string xml = "<?xml version=\"1.0\"?>\n<root>\n  <child>value</child>\n</root>";
        var processor = RuleTestHelpers.BuildRuleAndProcessor("XLARGE_LINE", "value", "string", "//child");
        Assert.True(_languages.FromFileNameOut("test.xml", out var info));

        // Force initial parse via normal API path
        var matchesNormal = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
        Assert.Single(matchesNormal);

        // Now parse separately to tamper with line info
        var xdoc = System.Xml.Linq.XDocument.Parse(xml, System.Xml.Linq.LoadOptions.SetLineInfo | System.Xml.Linq.LoadOptions.PreserveWhitespace);
        var child = xdoc.Root!.Element("child")!;

        // Remove existing annotations of same type (optional best-effort)
        var annotationType = CreateLineInfoAnnotation(1,1).GetType();
        var removeMethod = typeof(System.Xml.Linq.XObject)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "RemoveAnnotations" && m.IsGenericMethod && m.GetParameters().Length == 0);
        if (removeMethod != null)
        {
            try
            {
                removeMethod.MakeGenericMethod(annotationType).Invoke(child, null);
            }
            catch { /* ignore */ }
        }

        // Add a bogus huge line number
        child.AddAnnotation(CreateLineInfoAnnotation(9999, 1));

        // Re-run through TextContainer directly to exercise fallback logic
        var tc = new TextContainer(xml, "xml", _languages, null, "test.xml");
        var results = tc.GetStringFromXPath("//child", new Dictionary<string,string>()).ToList();
        var r = Assert.Single(results);
        Assert.Equal("value", r.Item1);
        Assert.InRange(r.Item2.Index, 0, xml.Length - r.Item2.Length);
    }

    [Fact]
    public void XPath_With_ZeroColumn_ClampsAndFindsValue()
    {
        const string xml = "<root><child>value</child></root>";
        var processor = RuleTestHelpers.BuildRuleAndProcessor("ZERO_COL", "value", "string", "//child");
        Assert.True(_languages.FromFileNameOut("test.xml", out var info));
        var matchesNormal = processor.AnalyzeFile(xml, new FileEntry("test.xml", new MemoryStream()), info);
        Assert.Single(matchesNormal);

        var xdoc = System.Xml.Linq.XDocument.Parse(xml, System.Xml.Linq.LoadOptions.SetLineInfo | System.Xml.Linq.LoadOptions.PreserveWhitespace);
        var child = xdoc.Root!.Element("child")!;

        var annotationType = CreateLineInfoAnnotation(1,1).GetType();
        var removeMethod = typeof(System.Xml.Linq.XObject)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "RemoveAnnotations" && m.IsGenericMethod && m.GetParameters().Length == 0);
        if (removeMethod != null)
        {
            try { removeMethod.MakeGenericMethod(annotationType).Invoke(child, null); } catch { }
        }

        child.AddAnnotation(CreateLineInfoAnnotation(1, 0)); // invalid column

        var tc = new TextContainer(xml, "xml", _languages, null, "test.xml");
        var results = tc.GetStringFromXPath("//child", new Dictionary<string,string>()).ToList();
        var r = Assert.Single(results);
        Assert.Equal("value", r.Item1);
        Assert.InRange(r.Item2.Index, 0, xml.Length - r.Item2.Length);
    }
}
