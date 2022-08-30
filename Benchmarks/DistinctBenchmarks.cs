using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.RulesEngine;

namespace Benchmarks;

[MemoryDiagnoser()]
public class DistinctBenchmarks
{
    public DistinctBenchmarks()
    {
    }
    IEnumerable<Rule> ruleSet = RuleSetUtils.GetDefaultRuleSet().GetAppInspectorRules();
    
    [Benchmark(Baseline = true)]
    public List<string> OldCode()
    {
        SortedDictionary<string, string> uniqueTags = new();
        List<string> outList = new();
        try
        {
            foreach (Rule? r in ruleSet)
            {
                //builds a list of unique tags
                foreach (string t in r?.Tags ?? Array.Empty<string>())
                {
                    if (uniqueTags.ContainsKey(t))
                    {
                        continue;
                    }
                    else
                    {
                        uniqueTags.Add(t, t);
                    }
                }
            }

            //generate results list
            foreach (string s in uniqueTags.Values)
            {
                outList.Add(s);
            }
        }
        catch (OpException e)
        {
            throw;
        }

        return outList;
    }

    [Benchmark]
    public List<string> HashSet()
    {
        HashSet<string> hashSet = new();
        foreach (Rule? r in ruleSet)
        {
            //builds a list of unique tags
            foreach (string t in r?.Tags ?? Array.Empty<string>())
            {
                hashSet.Add(t);
            }
        }

        List<string> theList = hashSet.ToList();
        theList.Sort();
        return theList;
    }
    
    [Benchmark]
    public List<string> WithLinq()
    {
        return ruleSet.SelectMany(x => x.Tags ?? Array.Empty<string>()).Distinct().OrderBy(x => x)
            .ToList();
    }
    
    [Benchmark]
    public List<string> WithLinqAndHashSet()
    {
        var theList = ruleSet.SelectMany(x => x.Tags ?? Array.Empty<string>()).ToHashSet().ToList();
        theList.Sort();
        return theList;
    }
}