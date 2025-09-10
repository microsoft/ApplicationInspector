using System;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace AppInspector.Tests.RuleProcessor;

public class YamlPathTests
{
    private const string arrayTestData = 
        @"some_array:
        - 0
        - 1
        - 2
        - 3";

    [Theory]
    [InlineData("some_array.[1:2]", 1, new int[]{1})]
    [InlineData("/some_array[1:2]", 1, new int[]{1})]
    [InlineData("/some_array/1", 1, new int[]{1})]
    [InlineData("/some_array/[1:2]", 1, new int[]{1})]
    [InlineData("/some_array/[1:4]", 3, new int[]{1, 2, 3})]
    [InlineData("/some_array/5", 0, new int[]{})]
    [InlineData("/some_array/[0:4]", 4, new int[]{0, 1, 2, 3})]
    [InlineData("/some_array/[-3:-2]", 1, new int[]{1})]
    public void TestArraySlicing(string yamlPath, int expectedNumMatches, int[] expectedFindings)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(arrayTestData));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.Query(yamlPath).ToList();
        Assert.Equal(expectedNumMatches, matching.Count());
        foreach (var expectedFinding in expectedFindings)
        {
        Assert.Contains(matching, x => x is YamlScalarNode ysc && int.TryParse(ysc.Value, out int ysv) && ysv == expectedFinding);
        }
    }
    
    private const string mapSliceTestData = 
        @"hash_name:
      a_key: 0
      b_key: 1
      c_key: 2
      d_key: 3
      e_key: 4";

    [Theory]
    [InlineData("hash_name.[a_key:b_key]", 2, new int[]{0, 1})]
    [InlineData("/hash_name[a_key:b_key]", 2, new int[]{0, 1})]
    [InlineData("/hash_name/d_key", 1, new int[]{3})]
    [InlineData("/hash_name/[a_key:b_key]", 2, new int[]{0, 1})]
    [InlineData("/hash_name/[a_key:d_key]", 4, new int[]{0, 1, 2, 3})]
    [InlineData("/hash_name/f_key", 0, new int[]{})]
    [InlineData("/hash_name/[a_key:f_key]", 0, new int[]{})]
    [InlineData("/hash_name/[a_key:e_key]", 5, new int[]{0, 1, 2, 3, 4})]
    public void TestMapSlicing(string yamlPath, int expectedNumMatches, int[] expectedFindings)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(mapSliceTestData));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.Query(yamlPath).ToList();
        Assert.Equal(expectedNumMatches, matching.Count());
        foreach (var expectedFinding in expectedFindings)
        {
            Assert.Contains(matching, x => x is YamlScalarNode ysc && int.TryParse(ysc.Value, out int ysv) && ysv == expectedFinding);
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
      tag with space: 7
      value_with_space: value with space
      other_values: 
        - 0
        - 5
        - 10";

    [Theory]
    [InlineData("products_array.**.[has_child(dimensions)]", 3)]
    [InlineData("products_array.**.dimensions['tag with space'==7]", 1)]
    [InlineData("products_array.**.dimensions[value_with_space=='value with space']", 1)]
    [InlineData("products_array.**.dimensions[other_values>=5]", 2)]
    [InlineData("products_array.**.[other_values>=5]", 2)]
    [InlineData("products_hash.**", 24)]
    [InlineData("products_array.**", 32)]
    [InlineData("products_array.[product=widget]", 1)]
    [InlineData("products_array.[availability.start.time=12:00]", 1)]
    [InlineData("products_array.[availability.start.missing=12:00]", 0)]
    [InlineData("products_array.[missing=present].start.[time=12:00]", 0)]
    [InlineData("products_array.*.dimensions[other_values>=5]", 2)]
    [InlineData("products_array.*.dimensions[width=9]", 1)]
    [InlineData("products_array.*.dimensions[weight=4]", 2)]
    [InlineData("products_array.*.dimensions[weight==4]", 2)]
    [InlineData("products_array.*.dimensions[weight == 4]", 2)]
    [InlineData("products_array.*.dimensions[weight<5]", 2)]
    [InlineData("products_array.*.dimensions[weight>4]", 1)]
    [InlineData("products_array.*.dimensions[weight<=4]", 2)]
    [InlineData("products_array.*.dimensions[weight>=4]", 3)]
    [InlineData("products_array.*.availability.start[date^2020]", 3)]
    [InlineData("products_array.*.availability.start[date$01]", 2)]
    [InlineData("products_array.*.availability.start[date%-10-]", 1)]
    [InlineData("products_hash.*.dimensions[width=9]", 1)]
    [InlineData("products_hash.*.dimensions[weight=4]", 2)]
    [InlineData("products_hash.*.dimensions[weight==4]", 2)]
    [InlineData("products_hash.*.dimensions[weight == 4]", 2)]
    [InlineData("products_hash.*.dimensions[weight<5]", 2)]
    [InlineData("products_hash.*.dimensions[weight>4]", 1)]
    [InlineData("products_hash.*.dimensions[weight<=4]", 2)]
    [InlineData("products_hash.*.dimensions[weight>=4]", 3)]
    [InlineData("products_hash.*.availability.start[date^2020]", 3)]
    [InlineData("products_hash.*.availability.start[date$01]", 2)]
    [InlineData("products_hash.*.availability.start[date%-10-]", 1)]
    [InlineData("/products_hash/*/availability/start[date=~2020.*]", 3)]
    [InlineData("products_hash.*.availability.start[date=~2020.*]", 3)]
    [InlineData("products_hash.*.availability.start[!date=~2020-01.*]", 2)]
    [InlineData("products_hash.*.availability.start[date!=~2020-01.*]", 2)]
    [InlineData("products_hash.*.availability.start[date=~2020-01.*]", 1)]
    [InlineData("products_hash.*.availability.start[date=~.*]", 3)]
    [InlineData("products_hash.*.availability.[start.date=~.*]", 3)]
    [InlineData("[products_hash.*.availability.start.date=~.*]", 3)]
    [InlineData("products_hash.*.availability.sta*.[date=~.*]", 3)]
    [InlineData("products_hash.*.availability.*rt.[date=~.*]", 3)]
    [InlineData("products_hash.*.availability.*a*.[date=~.*]", 3)]
    [InlineData("products_hash.*.availability.[start.missing=~.*]", 0)]
    [InlineData("products_hash.*.availability.[parent()]", 3)]
    [InlineData("products_array.*.availability.[parent()]", 3)]
    [InlineData("products_hash.*.availability.[parent(2)]", 1)]
    [InlineData("products_array.*.availability.[parent(2)]", 1)]
    [InlineData("products_hash.*.availability.[parent()].[parent()]", 1)]
    [InlineData("products_array.*.availability.[parent()].[parent()]", 1)]
    [InlineData("products_hash.*.availability.[name()]", 3)]
    [InlineData("products_array.*.availability.[name()]", 3)]
    public void TestMapQuery(string yamlPath, int expectedNumMatches)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(mapQueryTestData));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.Query(yamlPath);
        Assert.Equal(expectedNumMatches, matching.Count());
    }

    
    private const string setTestValue = @"--- !!set
    ? Ring
    ? Necklace
    ? Bracelet";
        
    [Theory]
    [InlineData("/Ring", 1)]
    public void TestSetQuery(string yamlPath, int expectedNumMatches)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(setTestValue));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.Query(yamlPath);
        Assert.Equal(expectedNumMatches, matching.Count());
    }
    
    private const string escapedKeysData = 
        @"hash_name:
      a/key: 0
      b.key: 1
      c\\key: 2
      d\\/key: 3
      e\\.key: 4";

    [Theory]
    [InlineData("/hash_name/a\\/key", 1, new int[]{0})]
    [InlineData("hash_name.a/key", 1, new int[]{0})]
    [InlineData("/hash_name/b.key", 1, new int[]{1})]
    [InlineData("hash_name.b\\.key", 1, new int[]{1})]
    [InlineData("hash_name.c\\\\key", 1, new int[]{2})]
    [InlineData("/hash_name/d\\\\\\/key", 1, new int[]{3})]
    [InlineData("hash_name.e\\\\\\.key", 1, new int[]{4})]
    public void EscapedKeysTests(string yamlPath, int expectedNumMatches, int[] expectedFindings)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(escapedKeysData));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.Query(yamlPath);
        Assert.Equal(expectedNumMatches, matching.Count());
        foreach (var expectedFinding in expectedFindings)
        {
            Assert.Contains(matching, x => x is YamlScalarNode ysc && int.TryParse(ysc.Value, out int ysv) && ysv == expectedFinding);
        }
    }

    private const string anchorTests = @"---
aliases:
  - &reusable_value This value can be used multiple times

anchored_hash: &anchor1
  with: child
  nodes: and values
  including: *reusable_value

non_anchored_hash:
  <<: *anchor1
  with: its
  own: children";

    [Theory]
    [InlineData("aliases[&reusable_value]", 1, "This value can be used multiple times")]
    [InlineData("/aliases[&reusable_value]", 1, "This value can be used multiple times")]
    [InlineData("&anchor1.&reusable_value", 1, "This value can be used multiple times")]
    [InlineData("/&anchor1/&reusable_value", 1, "This value can be used multiple times")]
    [InlineData("non_anchored_hash.&reusable_value", 1, "This value can be used multiple times")]
    [InlineData("/non_anchored_hash/&reusable_value", 1, "This value can be used multiple times")]
    [InlineData("non_anchored_hash.&anchor1.&reusable_value", 1, "This value can be used multiple times")]
    [InlineData("/non_anchored_hash/&anchor1/&reusable_value", 1, "This value can be used multiple times")]
    public void AnchorLiteral(string yamlPath, int expectedNumMatches, string expectedFirstScalarNode)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(anchorTests));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.Query(yamlPath).ToList();
        Assert.Equal(expectedNumMatches, matching.Count());
        Assert.True(matching.First() is YamlScalarNode);
        if (matching.First() is YamlScalarNode yamlScalarNode)
        {
            Assert.Equal(expectedFirstScalarNode, yamlScalarNode.Value);
        }
    }
        
    
    [Theory]
    [InlineData("&anchor1", 1, "with")]
    [InlineData("/&anchor1", 1, "with")]
    [InlineData("non_anchored_hash.&anchor1", 1, "with")]
    [InlineData("/non_anchored_hash/&anchor1", 1, "with")]
    public void AnchorReference(string yamlPath, int expectedNumMatches, string expectedFirstScalarNode)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(anchorTests));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.Query(yamlPath).ToList();
        Assert.Equal(expectedNumMatches, matching.Count());
        Assert.True(matching.First() is YamlMappingNode);
        if (matching.First() is YamlMappingNode yamlMappingNode)
        {
            if (yamlMappingNode.First().Key is YamlScalarNode yamlScalarNode)
            {
                Assert.Equal(expectedFirstScalarNode, yamlScalarNode.Value);
            }
        }
    }

    private const string minMaxData = @"---
    # Consistent Data Types
    prices_aoh:
      - product: doohickey
        price: 4.99
      - product: fob
        price: 4.99
      - product: whatchamacallit
        price: 9.95
      - product: widget
        price: 0.98
      - product: unknown

    prices_hash:
      doohickey:
        price: 4.99
      fob:
        price: 4.99
      whatchamacallit:
        price: 9.95
      widget:
        price: 0.98
      unknown:

    prices_array:
      - 4.99
      - 4.99
      - 9.95
      - 0.98
      - null";

    [Theory]
    [InlineData("/prices_aoh[max(price)]/price", 1, new string[]{"9.95"})]
    [InlineData("/prices_hash/[max(price)]/price", 1, new string[]{"9.95"})]
    [InlineData("/prices_array/[max()]", 1, new string[]{"9.95"})]
    [InlineData("/prices_aoh[min(price)]/price", 1, new string[]{"0.98"})]
    [InlineData("/prices_hash[min(price)]/price", 1, new string[]{"0.98"})]
    [InlineData("/prices_array/[min()]", 1, new string[]{"0.98"})]
    [InlineData("/prices_aoh[!max(price)]/price", 3, new string[]{"0.98", "4.99", "4.99"})]
    [InlineData("/prices_hash/[!max(price)]/price", 3, new string[]{"0.98", "4.99", "4.99"})]
    [InlineData("/prices_array/[!max()]", 4, new string[]{"0.98", "4.99", "4.99"})]
    [InlineData("/prices_aoh[!min(price)]/price", 3, new string[]{"9.95", "4.99", "4.99"})]
    [InlineData("/prices_hash[!min(price)]/price", 3, new string[]{"9.95", "4.99", "4.99"})]
    [InlineData("/prices_array/[!min()]", 4, new string[]{"9.95", "4.99", "4.99"})]
    public void MinMaxTests(string yamlPath, int expectedNumMatches, string[] expectedFindings)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(minMaxData));
        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        var matching = mapping.Query(yamlPath).ToList();
        Assert.Equal(expectedNumMatches, matching.Count());
        foreach (var expectedFinding in expectedFindings)
        {
            Assert.Contains(matching, x => x is YamlScalarNode ysc && decimal.TryParse(ysc.Value, out decimal ysv) && ysv == Decimal.Parse(expectedFinding));
        }
    }

    [Theory]
    [InlineData("(thing) + (other thing)", 1)]
    [InlineData("[notclosed=3", 1)]
    [InlineData("[!field!==value]", 1)]
    [InlineData("[field # value]", 1)]
    public void QueryValidatorTests(string queryToValidate, int expectedNumProblems)
    {
        Assert.Equal(expectedNumProblems, YamlPathExtensions.GetQueryProblems(queryToValidate).Count);
    }
}
