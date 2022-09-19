using System;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace AppInspector.YamlPath.Tests;

[TestClass]
public class UnitTest1
{
    private const string testData = 
        @"some_array:
    - 0
    - 1
    - 2
    - 3";
    
    [DataRow("/some_array/1", 1, new int[]{1})]
    [DataRow("/some_array/[1:2]", 1, new int[]{1})]
    [DataRow("/some_array/[1:4]", 3, new int[]{1, 2, 3})]
    [DataRow("/some_array/5", 0, new int[]{})]
    [DataRow("/some_array/[0:4]", 4, new int[]{0, 1, 2, 3})]
    [DataRow("/some_array/[-3:-2]", 1, new int[]{1})]

    [DataTestMethod]
    public void TestArraySlicing(string yamlPath, int expectedNumMatches, int[] expectedFindings)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(testData));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.YamlPathQuery(yamlPath);
        Assert.AreEqual(expectedNumMatches, matching.Count);
        foreach (var expectedFinding in expectedFindings)
        {
            Assert.IsTrue(matching.Any(x => x is YamlScalarNode ysc && int.TryParse(ysc.Value, out int ysv) && ysv == expectedFinding));
        }
    }
}