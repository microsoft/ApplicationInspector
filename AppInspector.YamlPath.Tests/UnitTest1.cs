using System;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace AppInspector.YamlPath.Tests;

[TestClass]
public class UnitTest1
{
    private const string testData = 
        @"some_array:
    - element1
    - element2
    - element3
    - element4";
    
    [TestMethod]
    public void TestMethod1()
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(testData));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.YamlPathQuery("/some_array/1");
        Console.WriteLine(matching.Count);
        Console.WriteLine(matching.Count);

    }
}