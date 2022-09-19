using System;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace AppInspector.YamlPath.Tests;

[TestClass]
public class UnitTest1
{
    private const string arrayTestData = 
        @"some_array:
    - 0
    - 1
    - 2
    - 3";
    [DataRow("some_array.[1:2]", 1, new int[]{1})]
    [DataRow("/some_array[1:2]", 1, new int[]{1})]
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
        yaml.Load(new StringReader(arrayTestData));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.YamlPathQuery(yamlPath);
        Assert.AreEqual(expectedNumMatches, matching.Count);
        foreach (var expectedFinding in expectedFindings)
        {
            Assert.IsTrue(matching.Any(x => x is YamlScalarNode ysc && int.TryParse(ysc.Value, out int ysv) && ysv == expectedFinding));
        }
    }
    
    private const string mapSliceTestData = 
        @"hash_name:
  a_key: 0
  b_key: 1
  c_key: 2
  d_key: 3
  e_key: 4";
    [DataRow("hash_name.[a_key:b_key]", 2, new int[]{0, 1})]
    [DataRow("/hash_name[a_key:b_key]", 2, new int[]{0, 1})]
    [DataRow("/hash_name/d_key", 1, new int[]{3})]
    [DataRow("/hash_name/[a_key:b_key]", 2, new int[]{0, 1})]
    [DataRow("/hash_name/[a_key:d_key]", 4, new int[]{0, 1, 2, 3})]
    [DataRow("/hash_name/f_key", 0, new int[]{})]
    [DataRow("/hash_name/[a_key:f_key]", 0, new int[]{})]
    [DataRow("/hash_name/[a_key:e_key]", 5, new int[]{0, 1, 2, 3, 4})]
    [DataTestMethod]
    public void TestMapSlicing(string yamlPath, int expectedNumMatches, int[] expectedFindings)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(mapSliceTestData));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.YamlPathQuery(yamlPath);
        Assert.AreEqual(expectedNumMatches, matching.Count);
        foreach (var expectedFinding in expectedFindings)
        {
            Assert.IsTrue(matching.Any(x => x is YamlScalarNode ysc && int.TryParse(ysc.Value, out int ysv) && ysv == expectedFinding));
        }
    }
    
    private const string mapQueryTestData = 
        @"products_hash:
  doodad:
    availability:
      start:
        date: 2020-10-10
        time: 08:00
      stop:
        date: 2020-10-29
        time: 17:00
    dimensions:
      width: 5
      height: 5
      depth: 5
      weight: 10
  doohickey:
    availability:
      start:
        date: 2020-08-01
        time: 10:00
      stop:
        date: 2020-09-25
        time: 10:00
    dimensions:
      width: 1
      height: 2
      depth: 3
      weight: 4
  widget:
    availability:
      start:
        date: 2020-01-01
        time: 12:00
      stop:
        date: 2020-01-01
        time: 16:00
    dimensions:
      width: 9
      height: 10
      depth: 1
      weight: 4
 
products_array:
  - product: doodad
    availability:
      start:
        date: 2020-10-10
        time: 08:00
      stop:
        date: 2020-10-29
        time: 17:00
    dimensions:
      width: 5
      height: 5
      depth: 5
      weight: 10
  - product: doohickey
    availability:
      start:
        date: 2020-08-01
        time: 10:00
      stop:
        date: 2020-09-25
        time: 10:00
    dimensions:
      width: 1
      height: 2
      depth: 3
      weight: 4
  - product: widget
    availability:
      start:
        date: 2020-01-01
        time: 12:00
      stop:
        date: 2020-01-01
        time: 16:00
    dimensions:
      width: 9
      height: 10
      depth: 1
      weight: 4
      other_values: 
        - 0
        - 5
        - 10";
    
    [DataRow("products_array.*.dimensions[other_values>=5]", 2)]
    [DataRow("products_array.*.dimensions[width=9]", 1)]
    [DataRow("products_array.*.dimensions[weight=4]", 2)]
    [DataRow("products_array.*.dimensions[weight==4]", 2)]
    [DataRow("products_array.*.dimensions[weight == 4]", 2)]
    [DataRow("products_array.*.dimensions[weight<5]", 2)]
    [DataRow("products_array.*.dimensions[weight>4]", 1)]
    [DataRow("products_array.*.dimensions[weight<=4]", 2)]
    [DataRow("products_array.*.dimensions[weight>=4]", 3)]
    [DataRow("products_array.*.availability.start[date^2020]", 3)]
    [DataRow("products_array.*.availability.start[date$01]", 2)]
    [DataRow("products_array.*.availability.start[date%-10-]", 1)]
    [DataRow("products_hash.*.dimensions[width=9]", 1)]
    [DataRow("products_hash.*.dimensions[weight=4]", 2)]
    [DataRow("products_hash.*.dimensions[weight==4]", 2)]
    [DataRow("products_hash.*.dimensions[weight == 4]", 2)]
    [DataRow("products_hash.*.dimensions[weight<5]", 2)]
    [DataRow("products_hash.*.dimensions[weight>4]", 1)]
    [DataRow("products_hash.*.dimensions[weight<=4]", 2)]
    [DataRow("products_hash.*.dimensions[weight>=4]", 3)]
    [DataRow("products_hash.*.availability.start[date^2020]", 3)]
    [DataRow("products_hash.*.availability.start[date$01]", 2)]
    [DataRow("products_hash.*.availability.start[date%-10-]", 1)]
    [DataRow("/products_hash/*/availability/start[date=~2020.*]", 3)]
    // [DataRow("products_hash.*.dimensions[weight=~4]", 2)]
    [DataTestMethod]
    public void TestMapQuery(string yamlPath, int expectedNumMatches)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(mapQueryTestData));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.YamlPathQuery(yamlPath);
        Assert.AreEqual(expectedNumMatches, matching.Count);
    }
}