using System;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
/// Comprehensive tests for XPath position accuracy in GetStringFromXPath method.
/// These tests address issue #621 - inconsistencies with XPath query position detection.
/// </summary>
public class XPathPositionTests
{
    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    #region Test Data

    /// <summary>
    /// XML with multiple elements containing the same text value to test position accuracy
    /// </summary>
    private const string XmlWithDuplicateValues = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<root>
    <metadata>
        <version>1.0.0</version>
        <description>Test description</description>
    </metadata>
    <dependencies>
        <dependency>
            <name>first-lib</name>
            <version>1.0.0</version>
        </dependency>
        <dependency>
            <name>second-lib</name>
            <version>2.0.0</version>
        </dependency>
        <dependency>
            <name>third-lib</name>
            <version>1.0.0</version>
        </dependency>
    </dependencies>
    <configuration>
        <setting name=""timeout"" value=""1.0.0""/>
        <setting name=""retries"" value=""5""/>
    </configuration>
</root>";

    /// <summary>
    /// XML with nested elements and attributes containing similar values - NON-NAMESPACED version for local-name() tests
    /// </summary>
    private const string XmlWithNestedDuplicates = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project>
    <groupId>com.example</groupId>
    <artifactId>test-project</artifactId>
    <version>1.0.0</version>
    
    <properties>
        <maven.compiler.source>11</maven.compiler.source>
        <maven.compiler.target>11</maven.compiler.target>
    </properties>
    
    <dependencies>
        <dependency>
            <groupId>org.junit.jupiter</groupId>
            <artifactId>junit-jupiter</artifactId>
            <version>5.8.2</version>
            <scope>test</scope>
        </dependency>
        <dependency>
            <groupId>org.mockito</groupId>
            <artifactId>mockito-core</artifactId>
            <version>4.6.1</version>
            <scope>test</scope>
        </dependency>
    </dependencies>
    
    <build>
        <plugins>
            <plugin>
                <groupId>org.apache.maven.plugins</groupId>
                <artifactId>maven-compiler-plugin</artifactId>
                <version>3.8.1</version>
                <configuration>
                    <source>11</source>
                    <target>11</target>
                </configuration>
            </plugin>
        </plugins>
    </build>
</project>";

    /// <summary>
    /// XML with Maven namespace for proper namespace handling tests
    /// </summary>
    private const string XmlWithMavenNamespace = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0""
         xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
         xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
    <modelVersion>4.0.0</modelVersion>
    
    <groupId>com.example</groupId>
    <artifactId>test-project</artifactId>
    <version>1.0.0</version>
    <packaging>jar</packaging>
    
    <properties>
        <maven.compiler.source>11</maven.compiler.source>
        <maven.compiler.target>11</maven.compiler.target>
    </properties>
    
    <dependencies>
        <dependency>
            <groupId>org.junit.jupiter</groupId>
            <artifactId>junit-jupiter</artifactId>
            <version>5.8.2</version>
            <scope>test</scope>
        </dependency>
        <dependency>
            <groupId>org.mockito</groupId>
            <artifactId>mockito-core</artifactId>
            <version>4.6.1</version>
            <scope>test</scope>
        </dependency>
    </dependencies>
    
    <build>
        <plugins>
            <plugin>
                <groupId>org.apache.maven.plugins</groupId>
                <artifactId>maven-compiler-plugin</artifactId>
                <version>3.8.1</version>
                <configuration>
                    <source>11</source>
                    <target>11</target>
                </configuration>
            </plugin>
        </plugins>
    </build>
</project>";

    /// <summary>
    /// XML with attributes that have the same values as element text
    /// </summary>
    private const string XmlWithAttributeAndElementDuplicates = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<catalog>
    <book id=""book1"" category=""fiction"">
        <title author=""Smith"">The Test Book</title>
        <author>Smith</author>
        <price currency=""USD"">19.99</price>
        <isbn>978-0123456789</isbn>
    </book>
    <book id=""book2"" category=""non-fiction"">
        <title author=""Johnson"">Learning Guide</title>
        <author>Johnson</author>
        <price currency=""USD"">29.99</price>
        <isbn>978-0987654321</isbn>
    </book>
    <metadata>
        <category>fiction</category>
        <category>non-fiction</category>
    </metadata>
</catalog>";

    /// <summary>
    /// XML with namespace declarations
    /// </summary>
    private const string XmlWithNamespaces = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
          xmlns:tools=""http://schemas.android.com/tools""
          package=""com.example.app"">
    
    <application android:debuggable=""true""
                 android:label=""@string/app_name""
                 tools:ignore=""HardcodedDebugMode"">
        
        <activity android:name="".MainActivity""
                  android:debuggable=""false""
                  android:exported=""true"">
            <intent-filter>
                <action android:name=""android.intent.action.MAIN"" />
                <category android:name=""android.intent.category.LAUNCHER"" />
            </intent-filter>
        </activity>
        
        <service android:name="".BackgroundService""
                 android:enabled=""true""
                 android:exported=""false"" />
    </application>
    
    <uses-permission android:name=""android.permission.INTERNET"" />
    <uses-permission android:name=""android.permission.ACCESS_NETWORK_STATE"" />
</manifest>";

    #endregion

    #region Test Rules

    private string CreateXPathRule(string id, string xpath, string pattern, string patternType = "string")
    {
        return $@"[
    {{
        ""id"": ""{id}"",
        ""name"": ""XPath Position Test Rule"",
        ""tags"": [""Test.XPath""],
        ""severity"": ""Critical"",
        ""description"": ""Test rule for XPath position accuracy"",
        ""patterns"": [
            {{
                ""pattern"": ""{pattern}"",
                ""type"": ""{patternType}"",
                ""confidence"": ""High"",
                ""scopes"": [""code""],
                ""xpaths"": [""{xpath}""]
            }}
        ]
    }}
]";
    }

    private string CreateXPathRuleWithNamespaces(string id, string xpath, string pattern, string patternType = "string")
    {
        return $@"[
    {{
        ""id"": ""{id}"",
        ""name"": ""XPath Namespace Test Rule"",
        ""tags"": [""Test.XPath.Namespace""],
        ""severity"": ""Critical"",
        ""description"": ""Test rule for XPath with namespaces"",
        ""patterns"": [
            {{
                ""pattern"": ""{pattern}"",
                ""type"": ""{patternType}"",
                ""confidence"": ""High"",
                ""scopes"": [""code""],
                ""xpaths"": [""{xpath}""],
                ""xpathnamespaces"": {{
                    ""android"": ""http://schemas.android.com/apk/res/android"",
                    ""tools"": ""http://schemas.android.com/tools""
                }}
            }}
        ]
    }}
]";
    }

    private string CreateXPathRuleWithMavenNamespaces(string id, string xpath, string pattern, string patternType = "string")
    {
        return $@"[
    {{
        ""id"": ""{id}"",
        ""name"": ""XPath Maven Namespace Test Rule"",
        ""tags"": [""Test.XPath.Maven.Namespace""],
        ""severity"": ""Critical"",
        ""description"": ""Test rule for XPath with Maven namespaces"",
        ""patterns"": [
            {{
                ""pattern"": ""{pattern}"",
                ""type"": ""{patternType}"",
                ""confidence"": ""High"",
                ""scopes"": [""code""],
                ""xpaths"": [""{xpath}""],
                ""xpathnamespaces"": {{
                    ""mvn"": ""http://maven.apache.org/POM/4.0.0"",
                    ""xsi"": ""http://www.w3.org/2001/XMLSchema-instance""
                }}
            }}
        ]
    }}
]";
    }

    #endregion

    #region Duplicate Value Tests

    [Fact]
    public void XPath_ElementWithDuplicateValues_ReturnsCorrectPositions()
    {
        // Test that when multiple elements have the same text content,
        // each match returns the correct position for its specific element
        
        var rule = CreateXPathRule("XPATH_DUPLICATE_001", "//version", "1.0.0");
        
        RuleSet rules = new();
        rules.AddString(rule, "XPathPositionTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(XmlWithDuplicateValues, new FileEntry("test.xml", new MemoryStream()), info);
            
            // Should find 3 matches for "1.0.0" in different version elements
            Assert.Equal(3, matches.Count);
            
            // Verify each match points to the correct location
            var expectedPositions = new[]
            {
                XmlWithDuplicateValues.IndexOf("<version>1.0.0</version>") + "<version>".Length, // metadata version
                XmlWithDuplicateValues.IndexOf("<version>1.0.0</version>", XmlWithDuplicateValues.IndexOf("<dependency>")) + "<version>".Length, // first dependency
                XmlWithDuplicateValues.LastIndexOf("<version>1.0.0</version>") + "<version>".Length // third dependency
            };
            
            var actualPositions = matches.Select(m => m.Boundary.Index).OrderBy(p => p).ToArray();
            
            for (int i = 0; i < expectedPositions.Length; i++)
            {
                Assert.Equal(expectedPositions[i], actualPositions[i]);
                Assert.Equal(5, matches[i].Boundary.Length); // Length of "1.0.0"
                Assert.Equal("1.0.0", matches[i].Sample);
            }
        }
        else
        {
            Assert.Fail("Failed to get language info for test.xml");
        }
    }

    [Fact]
    public void XPath_AttributeWithDuplicateValues_ReturnsCorrectPositions()
    {
        // Test that attribute values are positioned correctly when duplicates exist
        
        var rule = CreateXPathRule("XPATH_ATTR_001", "//@currency", "USD");
        
        RuleSet rules = new();
        rules.AddString(rule, "XPathPositionTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("catalog.xml", out var info))
        {
            var matches = processor.AnalyzeFile(XmlWithAttributeAndElementDuplicates, new FileEntry("catalog.xml", new MemoryStream()), info);
            
            // Should find 2 matches for "USD" in currency attributes
            Assert.Equal(2, matches.Count);
            
            // Verify positions are correct for both currency attributes
            foreach (var match in matches)
            {
                Assert.Equal(3, match.Boundary.Length); // Length of "USD"
                Assert.Equal("USD", match.Sample);
                
                // Verify the match is actually within a currency attribute
                var contextStart = Math.Max(0, match.Boundary.Index - 20);
                var contextEnd = Math.Min(XmlWithAttributeAndElementDuplicates.Length, match.Boundary.Index + 20);
                var context = XmlWithAttributeAndElementDuplicates[contextStart..contextEnd];
                Assert.Contains("currency=", context);
            }
        }
        else
        {
            Assert.Fail("Failed to get language info for catalog.xml");
        }
    }

    [Fact]
    public void XPath_ComplexPathWithDuplicates_ReturnsCorrectPosition()
    {
        // Test complex XPath expressions that should match specific elements despite duplicates
        
        var rule = CreateXPathRule("XPATH_COMPLEX_001", 
            "//dependency[name='first-lib']/version", "1.0.0");
        
        RuleSet rules = new();
        rules.AddString(rule, "XPathPositionTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(XmlWithDuplicateValues, new FileEntry("test.xml", new MemoryStream()), info);
            
            // Should find exactly 1 match for the version of first-lib dependency
            Assert.Single(matches);
            
            var match = matches[0];
            Assert.Equal("1.0.0", match.Sample);
            Assert.Equal(5, match.Boundary.Length);
            
            // Verify this is specifically the first-lib dependency version, not metadata or third-lib
            var contextStart = Math.Max(0, match.Boundary.Index - 100);
            var contextEnd = Math.Min(XmlWithDuplicateValues.Length, match.Boundary.Index + 50);
            var context = XmlWithDuplicateValues[contextStart..contextEnd];
            Assert.Contains("first-lib", context);
        }
        else
        {
            Assert.Fail("Failed to get language info for test.xml");
        }
    }

    #endregion

    #region Namespace Tests

    [Fact]
    public void XPath_WithNamespaces_ReturnsCorrectPosition()
    {
        // Test XPath with namespace prefixes
        
        var rule = CreateXPathRuleWithNamespaces("XPATH_NS_001", 
            "//application/@android:debuggable", "true");
        
        RuleSet rules = new();
        rules.AddString(rule, "XPathNamespaceTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("AndroidManifest.xml", out var info))
        {
            var matches = processor.AnalyzeFile(XmlWithNamespaces, new FileEntry("AndroidManifest.xml", new MemoryStream()), info);
            
            // Should find the android:debuggable="true" on application element
            Assert.Single(matches);
            
            var match = matches[0];
            Assert.Equal("true", match.Sample);
            Assert.Equal(4, match.Boundary.Length);
            
            // Verify this is the application-level debuggable, not the activity-level one
            var contextStart = Math.Max(0, match.Boundary.Index - 50);
            var contextEnd = Math.Min(XmlWithNamespaces.Length, match.Boundary.Index + 30);
            var context = XmlWithNamespaces[contextStart..contextEnd];
            Assert.Contains("application", context);
            Assert.DoesNotContain("activity", context);
        }
        else
        {
            Assert.Fail("Failed to get language info for AndroidManifest.xml");
        }
    }

    [Fact]
    public void XPath_WithNamespaces_MultipleDuplicateAttributes_ReturnsCorrectPositions()
    {
        // Test XPath that matches multiple attributes with same name but different values
        
        var rule = CreateXPathRuleWithNamespaces("XPATH_NS_002", 
            "//@android:debuggable", "true|false", "regex");
        
        RuleSet rules = new();
        rules.AddString(rule, "XPathNamespaceTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("AndroidManifest.xml", out var info))
        {
            var matches = processor.AnalyzeFile(XmlWithNamespaces, new FileEntry("AndroidManifest.xml", new MemoryStream()), info);
            
            // Should find both android:debuggable attributes (true on application, false on activity)
            Assert.Equal(2, matches.Count);
            
            var trueMatch = matches.First(m => m.Sample == "true");
            var falseMatch = matches.First(m => m.Sample == "false");
            
            Assert.Equal(4, trueMatch.Boundary.Length);
            Assert.Equal(5, falseMatch.Boundary.Length);
            
            // Verify positions are correct
            Assert.True(trueMatch.Boundary.Index < falseMatch.Boundary.Index, 
                "Application debuggable should come before activity debuggable");
        }
        else
        {
            Assert.Fail("Failed to get language info for AndroidManifest.xml");
        }
    }

    #endregion

    #region Complex Maven POM Tests

    [Fact]
    public void XPath_MavenPOM_LocalName_SpecificDependencyVersion_ReturnsCorrectPosition()
    {
        // Test using local-name() to ignore namespaces - uses non-namespaced XML for clarity
        
        var rule = CreateXPathRule("XPATH_MAVEN_001", 
            "//*[local-name()='dependency'][*[local-name()='artifactId']='junit-jupiter']/*[local-name()='version']", "5.8.2");
        
        RuleSet rules = new();
        rules.AddString(rule, "XPathMavenTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = processor.AnalyzeFile(XmlWithNestedDuplicates, new FileEntry("pom.xml", new MemoryStream()), info);
            
            Assert.Single(matches);
            
            var match = matches[0];
            Assert.Equal("5.8.2", match.Sample);
            Assert.Equal(5, match.Boundary.Length);
            
            // Verify this is specifically the JUnit Jupiter dependency version
            var contextStart = Math.Max(0, match.Boundary.Index - 200);
            var contextEnd = Math.Min(XmlWithNestedDuplicates.Length, match.Boundary.Index + 50);
            var context = XmlWithNestedDuplicates[contextStart..contextEnd];
            Assert.Contains("junit-jupiter", context);
            Assert.DoesNotContain("mockito", context);
        }
        else
        {
            Assert.Fail("Failed to get language info for pom.xml");
        }
    }

    [Fact]
    public void XPath_MavenPOM_LocalName_AllDependencyVersions_ReturnsCorrectPositions()
    {
        // Test using local-name() to find all dependency versions - uses non-namespaced XML
        
        var rule = CreateXPathRule("XPATH_MAVEN_002", 
            "//*[local-name()='dependencies']//*[local-name()='version']", "\\\\d+\\\\.\\\\d+\\\\.\\\\d+", "regex");
        
        RuleSet rules = new();
        rules.AddString(rule, "XPathMavenTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = processor.AnalyzeFile(XmlWithNestedDuplicates, new FileEntry("pom.xml", new MemoryStream()), info);
            
            // Should find 2 versions under dependencies: junit (5.8.2), mockito (4.6.1)
            Assert.Equal(2, matches.Count);
            
            var versions = matches.Select(m => m.Sample).OrderBy(v => v).ToArray();
            Assert.Contains("4.6.1", versions); // mockito
            Assert.Contains("5.8.2", versions); // junit
            
            // Verify each position is correct
            foreach (var match in matches)
            {
                var actualText = XmlWithNestedDuplicates.Substring(match.Boundary.Index, match.Boundary.Length);
                Assert.Equal(match.Sample, actualText);
            }
        }
        else
        {
            Assert.Fail("Failed to get language info for pom.xml");
        }
    }

    [Fact]
    public void XPath_MavenPOM_WithNamespaces_SpecificDependencyVersion_ReturnsCorrectPosition()
    {
        // Test proper namespace handling for Maven POM with default namespace
        
        var rule = CreateXPathRuleWithMavenNamespaces("XPATH_MAVEN_NS_001", 
            "//mvn:dependency[mvn:artifactId='junit-jupiter']/mvn:version", "5.8.2");
        
        RuleSet rules = new();
        rules.AddString(rule, "XPathMavenNamespaceTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = processor.AnalyzeFile(XmlWithMavenNamespace, new FileEntry("pom.xml", new MemoryStream()), info);
            
            Assert.Single(matches);
            
            var match = matches[0];
            Assert.Equal("5.8.2", match.Sample);
            Assert.Equal(5, match.Boundary.Length);
            
            // Verify this is specifically the JUnit Jupiter dependency version
            var contextStart = Math.Max(0, match.Boundary.Index - 200);
            var contextEnd = Math.Min(XmlWithMavenNamespace.Length, match.Boundary.Index + 50);
            var context = XmlWithMavenNamespace[contextStart..contextEnd];
            Assert.Contains("junit-jupiter", context);
            Assert.DoesNotContain("mockito", context);
        }
        else
        {
            Assert.Fail("Failed to get language info for pom.xml");
        }
    }

    [Fact]
    public void XPath_MavenPOM_WithNamespaces_AllDependencyVersions_ReturnsCorrectPositions()
    {
        // Test proper namespace handling for all dependency versions
        
        var rule = CreateXPathRuleWithMavenNamespaces("XPATH_MAVEN_NS_002", 
            "//mvn:dependencies//mvn:version", "\\\\d+\\\\.\\\\d+\\\\.\\\\d+", "regex");
        
        RuleSet rules = new();
        rules.AddString(rule, "XPathMavenNamespaceTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = processor.AnalyzeFile(XmlWithMavenNamespace, new FileEntry("pom.xml", new MemoryStream()), info);
            
            // Should find 2 versions under dependencies: junit (5.8.2), mockito (4.6.1)
            Assert.Equal(2, matches.Count);
            
            var versions = matches.Select(m => m.Sample).OrderBy(v => v).ToArray();
            Assert.Contains("4.6.1", versions); // mockito
            Assert.Contains("5.8.2", versions); // junit
            
            // Verify each position is correct
            foreach (var match in matches)
            {
                var actualText = XmlWithMavenNamespace.Substring(match.Boundary.Index, match.Boundary.Length);
                Assert.Equal(match.Sample, actualText);
            }
        }
        else
        {
            Assert.Fail("Failed to get language info for pom.xml");
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void XPath_EmptyElements_HandledCorrectly()
    {
        var xmlWithEmptyElements = @"<?xml version=""1.0""?>
<root>
    <empty></empty>
    <selfClosing/>
    <withContent>actual content</withContent>
    <empty></empty>
</root>";
        
        var rule = CreateXPathRule("XPATH_EMPTY_001", "//withContent", "actual content");
        
        RuleSet rules = new();
        rules.AddString(rule, "XPathEmptyTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xmlWithEmptyElements, new FileEntry("test.xml", new MemoryStream()), info);
            
            Assert.Single(matches);
            
            var match = matches[0];
            Assert.Equal("actual content", match.Sample);
            Assert.Equal(14, match.Boundary.Length);
            
            // Verify position is correct
            var actualText = xmlWithEmptyElements.Substring(match.Boundary.Index, match.Boundary.Length);
            Assert.Equal("actual content", actualText);
        }
        else
        {
            Assert.Fail("Failed to get language info for test.xml");
        }
    }

    [Fact]
    public void XPath_WhitespaceInContent_PreservesCorrectPosition()
    {
        var xmlWithWhitespace = @"<?xml version=""1.0""?>
<root>
    <item>
        value with spaces    
    </item>
    <item>compact</item>
</root>";
        
        var rule = CreateXPathRule("XPATH_WHITESPACE_001", "//item[contains(text(), 'spaces')]", "value with spaces", "string");
        
        RuleSet rules = new();
        rules.AddString(rule, "XPathWhitespaceTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("test.xml", out var info))
        {
            var matches = processor.AnalyzeFile(xmlWithWhitespace, new FileEntry("test.xml", new MemoryStream()), info);
            
            Assert.Single(matches);
            
            var match = matches[0];
            // The XPath returns the actual content including whitespace, but the pattern matches the trimmed content
            // Since we're using PreserveWhitespace, the full text node content is returned
            Assert.Contains("value with spaces", match.Sample);
            
            // Verify the position points to the correct location in the document  
            var actualTextAtPosition = xmlWithWhitespace.Substring(match.Boundary.Index, match.Boundary.Length);
            Assert.Equal(match.Sample, actualTextAtPosition);
            
            // Platform-independent verification: normalize line endings for comparison
            var normalizedSample = match.Sample.Replace("\r\n", "\n").Replace("\r", "\n");
            var normalizedActual = actualTextAtPosition.Replace("\r\n", "\n").Replace("\r", "\n");
            Assert.Equal(normalizedSample, normalizedActual);
            
            // Additional verification: the content should contain our target text regardless of line endings
            Assert.True(normalizedSample.Contains("value with spaces"), 
                $"Expected normalized sample to contain 'value with spaces', but got: '{normalizedSample}'");
        }
        else
        {
            Assert.Fail("Failed to get language info for test.xml");
        }
    }

    #endregion

    #region Regression Tests for Issue #621

    [Fact]
    public void XPath_Issue621_LocalName_ComplexVersionQuery_ReturnsCorrectPosition()
    {
        // Direct test for the issue reported in #621 - using local-name() with non-namespaced XML
        
        var complexXPathRule = @"[
    {
        ""name"": ""TEST xpath complex issue 621 - local-name()"",
        ""description"": ""Detects the use of specific version patterns using local-name() to ignore namespaces"",
        ""id"": ""XPATH621001"",
        ""applies_to"": [
            ""pom.xml""
        ],
        ""tags"": [
            ""XPATH.Issue621.LocalName""
        ],
        ""severity"": ""critical"",
        ""patterns"": [
            {
                ""pattern"": ""5\\..+"",
                ""type"": ""regex"",
                ""scopes"": [
                    ""code""
                ],
                ""modifiers"": [],
                ""confidence"": ""high"",
                ""xpaths"": [""//*[local-name(.)='artifactId' and text()='junit-jupiter']/parent::*/*[local-name(.)='version']""]
            }
        ]
    }
]";

        RuleSet rules = new();
        rules.AddString(complexXPathRule, "XPathIssue621Test");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = processor.AnalyzeFile(XmlWithNestedDuplicates, new FileEntry("pom.xml", new MemoryStream()), info);
            
            Assert.Single(matches);
            
            var match = matches[0];
            
            // The key fix: position should point to the actual matched content, not document start
            var expectedPosition = XmlWithNestedDuplicates.IndexOf("5.8.2");
            Assert.True(expectedPosition > 0, "Version should be found in the document");
            
            // These assertions verify the bug fix for issue #621
            Assert.Equal(expectedPosition, match.Boundary.Index);
            Assert.Equal(5, match.Boundary.Length); // Length of "5.8.2"
            Assert.Equal("5.8.2", match.Sample);
            
            // Additional verification: ensure the position is actually within the JUnit dependency
            var contextStart = Math.Max(0, match.Boundary.Index - 100);
            var contextEnd = Math.Min(XmlWithNestedDuplicates.Length, match.Boundary.Index + 100);
            var context = XmlWithNestedDuplicates[contextStart..contextEnd];
            Assert.Contains("junit-jupiter", context);
        }
        else
        {
            Assert.Fail("Failed to get language info for pom.xml");
        }
    }

    [Fact]
    public void XPath_Issue621_WithNamespaces_ComplexVersionQuery_ReturnsCorrectPosition()
    {
        // Test the same Issue #621 scenario but with proper namespace handling
        
        var complexNamespacedXPathRule = @"[
    {
        ""name"": ""TEST xpath complex issue 621 - with namespaces"",
        ""description"": ""Detects the use of specific version patterns using proper namespace handling"",
        ""id"": ""XPATH621002"",
        ""applies_to"": [
            ""pom.xml""
        ],
        ""tags"": [
            ""XPATH.Issue621.Namespaced""
        ],
        ""severity"": ""critical"",
        ""patterns"": [
            {
                ""pattern"": ""5\\..+"",
                ""type"": ""regex"",
                ""scopes"": [
                    ""code""
                ],
                ""modifiers"": [],
                ""confidence"": ""high"",
                ""xpaths"": [""//mvn:dependency[mvn:artifactId='junit-jupiter']/mvn:version""],
                ""xpathnamespaces"": {
                    ""mvn"": ""http://maven.apache.org/POM/4.0.0""
                }
            }
        ]
    }
]";

        RuleSet rules = new();
        rules.AddString(complexNamespacedXPathRule, "XPathIssue621NamespaceTest");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
        
        if (_languages.FromFileNameOut("pom.xml", out var info))
        {
            var matches = processor.AnalyzeFile(XmlWithMavenNamespace, new FileEntry("pom.xml", new MemoryStream()), info);
            
            Assert.Single(matches);
            
            var match = matches[0];
            
            // The key fix: position should point to the actual matched content, not document start
            var expectedPosition = XmlWithMavenNamespace.IndexOf("5.8.2");
            Assert.True(expectedPosition > 0, "Version should be found in the document");
            
            // These assertions verify the bug fix for issue #621 with proper namespace handling
            Assert.Equal(expectedPosition, match.Boundary.Index);
            Assert.Equal(5, match.Boundary.Length); // Length of "5.8.2"
            Assert.Equal("5.8.2", match.Sample);
            
            // Additional verification: ensure the position is actually within the JUnit dependency
            var contextStart = Math.Max(0, match.Boundary.Index - 100);
            var contextEnd = Math.Min(XmlWithMavenNamespace.Length, match.Boundary.Index + 100);
            var context = XmlWithMavenNamespace[contextStart..contextEnd];
            Assert.Contains("junit-jupiter", context);
        }
        else
        {
            Assert.Fail("Failed to get language info for pom.xml");
        }
    }

    #endregion

    #region New Edge Case Tests

    /// <summary>
    /// Test edge cases that would fail with hardcoded 50/200 search windows
    /// </summary>
    [Fact]
    public void XPath_EdgeCases_RefactoredMethodHandlesBetter()
    {
        // XML with long attribute names and multi-line attributes that would exceed the old 50/200 limits
        var xmlWithLongAttributes = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<configuration>
    <feature veryLongAttributeNameThatExceedsFiftyCharactersEasily=""test-value""
             anotherLongAttributeNameForTestingPurposes=""another-value""
             shortAttr=""target-value""/>
    <element>
        <nestedElement someVeryLongAttributeNameThatWouldHaveBeenMissedByTheOldSearchWindow=""target-value""
                       additionalAttribute=""other-value"">
            <content>Some text content</content>
        </nestedElement>
    </element>
</configuration>";

        if (_languages.FromFileNameOut("test.xml", out var langInfo))
        {
            var tc = new TextContainer(xmlWithLongAttributes, langInfo.Name, _languages);
            
            // Test finding attributes with very long names that would exceed the old 50-char backward search
            var results = tc.GetStringFromXPath("//@someVeryLongAttributeNameThatWouldHaveBeenMissedByTheOldSearchWindow", new()).ToArray();
            
            Assert.Single(results);
            var (value, boundary) = results[0];
            Assert.Equal("target-value", value);
            
            // Verify the position is correct by extracting the text at that position
            var extractedText = xmlWithLongAttributes.Substring(boundary.Index, boundary.Length);
            Assert.Equal("target-value", extractedText);
            
            // Verify it's in the correct context (should be in the nestedElement)
            var contextStart = Math.Max(0, boundary.Index - 100);
            var contextEnd = Math.Min(xmlWithLongAttributes.Length, boundary.Index + 100);
            var context = xmlWithLongAttributes[contextStart..contextEnd];
            Assert.Contains("nestedElement", context);
            Assert.Contains("someVeryLongAttributeNameThatWouldHaveBeenMissedByTheOldSearchWindow", context);
        }
    }

    /// <summary>
    /// Test attributes with values that appear multiple times in the document
    /// </summary>
    [Fact]
    public void XPath_AttributeValueDuplicates_ReturnsCorrectPosition()
    {
        var xmlWithDuplicateValues = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<root>
    <element name=""common-value"">common-value</element>
    <items>
        <item id=""common-value"" type=""first"">Some content</item>
        <item id=""different-value"" type=""second"">common-value</item>
        <item id=""another-value"" type=""common-value"">Other content</item>
    </items>
</root>";

        if (_languages.FromFileNameOut("test.xml", out var langInfo))
        {
            var tc = new TextContainer(xmlWithDuplicateValues, langInfo.Name, _languages);
            
            // Get all @id attributes with value "common-value"
            var results = tc.GetStringFromXPath("//item[@id='common-value']/@id", new()).ToArray();
            
            Assert.Single(results);
            var (value, boundary) = results[0];
            Assert.Equal("common-value", value);
            
            // Verify this is the first item's id attribute, not the element text or other occurrences
            var extractedText = xmlWithDuplicateValues.Substring(boundary.Index, boundary.Length);
            Assert.Equal("common-value", extractedText);
            
            // Verify it's in the correct attribute context (should be in the id attribute, not content)
            var lineStart = xmlWithDuplicateValues.LastIndexOf('\n', boundary.Index) + 1;
            var lineEnd = xmlWithDuplicateValues.IndexOf('\n', boundary.Index);
            if (lineEnd == -1) lineEnd = xmlWithDuplicateValues.Length;
            var line = xmlWithDuplicateValues[lineStart..lineEnd];
            
            Assert.Contains("id=\"common-value\"", line);
            Assert.Contains("type=\"first\"", line);
        }
    }

    /// <summary>
    /// Test multi-line attributes that would fail with the old fixed search window
    /// </summary>
    [Fact]
    public void XPath_MultiLineAttributes_HandledCorrectly()
    {
        var xmlWithMultiLineAttr = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<configuration>
    <component name=""test-component""
               description=""This is a very long description that spans multiple lines 
                           and contains lots of detailed information about the component
                           including usage instructions and examples""
               version=""1.2.3""
               enabled=""true"">
        <settings>
            <setting key=""timeout"" value=""30000""/>
        </settings>
    </component>
</configuration>";

        if (_languages.FromFileNameOut("test.xml", out var langInfo))
        {
            var tc = new TextContainer(xmlWithMultiLineAttr, langInfo.Name, _languages);
            
            // Test finding the version attribute after a very long multi-line description
            var results = tc.GetStringFromXPath("//component/@version", new()).ToArray();
            
            Assert.Single(results);
            var (value, boundary) = results[0];
            Assert.Equal("1.2.3", value);
            
            // Verify the position is correct
            var extractedText = xmlWithMultiLineAttr.Substring(boundary.Index, boundary.Length);
            Assert.Equal("1.2.3", extractedText);
            
            // Verify it's in the version attribute context
            var contextStart = Math.Max(0, boundary.Index - 20);
            var contextEnd = Math.Min(xmlWithMultiLineAttr.Length, boundary.Index + 20);
            var context = xmlWithMultiLineAttr[contextStart..contextEnd];
            Assert.Contains("version=\"1.2.3\"", context);
        }
    }

    #endregion

    #region Cross-Platform Tests        
        [Fact]
        public void XPath_CrossPlatformLineEndings_BothUnixAndWindows()
        {
            // Test Unix line endings (\n)
            var unixXml = "<root>\n  <item>value</item>\n</root>";
            var unixContainer = new TextContainer(unixXml, "xml", _languages);
            
            // Test Windows line endings (\r\n)
            var windowsXml = "<root>\r\n  <item>value</item>\r\n</root>";
            var windowsContainer = new TextContainer(windowsXml, "xml", _languages);
            
            // Verify line boundary calculation is correct for both formats
            Assert.True(unixContainer.LineEnds.Count > 1);
            Assert.True(windowsContainer.LineEnds.Count > 1);
            
            // Unix: line ends should be at \n positions
            Assert.Equal(6, unixContainer.LineEnds[1]); // Position of first \n
            
            // Windows: line ends should be at \r positions (before \n) 
            Assert.Equal(6, windowsContainer.LineEnds[1]); // Position of first \r (before \r\n)
            
            // Test that both can extract content correctly
            var unixExtraction = unixContainer.GetStringFromXPath("//item", new()).ToArray();
            var windowsExtraction = windowsContainer.GetStringFromXPath("//item", new()).ToArray();
            
            Assert.Single(unixExtraction);
            Assert.Single(windowsExtraction);
            Assert.Contains("value", unixExtraction.First().Item1);
            Assert.Contains("value", windowsExtraction.First().Item1);
        }

    #endregion
}
