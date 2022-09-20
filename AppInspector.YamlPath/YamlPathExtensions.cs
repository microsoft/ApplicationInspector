﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace Microsoft.ApplicationInspector.ExtensionMethods;

public static class YamlPathExtensions
{
    private const char EscapeCharacter = '\\';

    /// <summary>
    ///     Mapping between the string literals for the search operators and their enums
    /// </summary>
    private static readonly Dictionary<string, SearchOperatorEnum> StringToOperatorMapping =
        new()
        {
            { "==", SearchOperatorEnum.Equals },
            { "=", SearchOperatorEnum.Equals },
            { "=~", SearchOperatorEnum.Regex },
            { "<=", SearchOperatorEnum.LessThanOrEqual },
            { ">=", SearchOperatorEnum.GreaterThanOrEqual },
            { "<", SearchOperatorEnum.LessThan },
            { ">", SearchOperatorEnum.GreaterThan },
            { "^", SearchOperatorEnum.StartsWith },
            { "$", SearchOperatorEnum.EndsWith },
            { "%", SearchOperatorEnum.Contains }
        };

    /// <summary>
    ///     Get all the <see cref="YamlNode" /> that match the provided yamlPath query.
    /// </summary>
    /// <param name="yamlNode">The YamlMappingNode to operate on</param>
    /// <param name="yamlPath">The YamlPath query to use</param>
    /// <returns>An <see cref="List{YamlNode}" /> of the matching nodes</returns>
    public static List<YamlNode> Query(this YamlNode yamlNode, string yamlPath)
    {
        // TODO: validate query
        var navigationElements = GenerateNavigationElements(yamlPath);

        var currentNodes = new List<YamlNode> { yamlNode };

        // Iteratively walk using the navigation 
        for (var i = 0; i < navigationElements.Count; i++)
        {
            // The list of nodes we can be in after parsing the next nav element
            var nextNodes = new List<YamlNode>();
            foreach (var currentNode in currentNodes)
                // https://github.com/wwkimball/yamlpath/wiki/Wildcard-Segments
                // The ** operator has two behaviors. If it is the last token it captures all Scalar Leaves recursively
                // If it is not the last token it matches any sequence of paths as long as subsequent tokens match
            {
                if (navigationElements[i] == "**")
                    // If this is the last element then just get all the leaves
                {
                    nextNodes.AddRange(i == navigationElements.Count - 1
                        ? GetLeaves(currentNode)
                        // If its not the last, we instead advance the position to every child through all levels recursively
                        // Then we will filter by all the subsequent components
                        : RecursiveGetAllNodes(currentNode));
                }
                else
                    // Advance the current node to all possible nodes with the navigation element
                {
                    nextNodes.AddRange(AdvanceNode(currentNode, navigationElements[i]));
                }
            }

            // Nothing matched the next sequence, so stop processing.
            if (!nextNodes.Any())
            {
                return new List<YamlNode>();
            }

            currentNodes = nextNodes;
        }

        return currentNodes;
    }

    /// <summary>
    ///     Select elements out of the mapping node based on a single path component
    /// </summary>
    /// <param name="yamlMappingNode">A mapping node to query</param>
    /// <param name="yamlPathComponent">A single path component. <see cref="GenerateNavigationElements" /></param>
    /// <returns>
    ///     A <see cref="List{YamlNode}" /> of the children of the provided <see cref="YamlMappingNode" /> which match the
    ///     query.
    /// </returns>
    private static IEnumerable<YamlNode> MappingNodeQuery(this YamlMappingNode yamlMappingNode,
        string yamlPathComponent)
    {
        var outNodes = new List<YamlNode>();
        // Remove braces
        var expr = yamlPathComponent.Trim('[', ']');
        // Maps support slices based on the key name
        // https://github.com/wwkimball/yamlpath/wiki/Segment:-Hash-Slices
        if (expr.Contains(':'))
        {
            var components = expr.Split(':');
            if (components.Length != 2)
            {
                return Enumerable.Empty<YamlNode>();
            }

            // If we have found the start key yet
            var foundStartKey = false;

            // This is an ordered set
            foreach (var childPair in yamlMappingNode.Children)
            {
                if (!foundStartKey)
                {
                    // Find the start key
                    if (childPair.Key is YamlScalarNode yamlScalarKey &&
                        (yamlScalarKey.Value?.Equals(components[0]) ?? false))
                    {
                        foundStartKey = true;
                        outNodes.Add(childPair.Value);
                    }
                }
                else
                {
                    outNodes.Add(childPair.Value);
                    // If we find the end, we return
                    if (childPair.Key is YamlScalarNode yamlScalarKey &&
                        (yamlScalarKey.Value?.Equals(components[1]) ?? false))
                    {
                        return outNodes;
                    }
                }
            }

            // If we get here we did not find the end, so the selection is not valid
            return Enumerable.Empty<YamlNode>();
        }

        // If it wasn't a slice it might be an expression
        // https://github.com/wwkimball/yamlpath/wiki/Search-Expressions
        if (yamlPathComponent.StartsWith('[') && yamlPathComponent.EndsWith(']'))
        {
            return PerformOperation(yamlMappingNode, yamlPathComponent);
        }

        // If it wasn't a slice or an expression it might be a wildcard
        // Wild Cards https://github.com/wwkimball/yamlpath/wiki/Wildcard-Segments
        if (yamlPathComponent == "*")
        {
            return yamlMappingNode.Children.Values;
        }

        // TODO: Other operand meanings https://github.com/wwkimball/yamlpath/wiki/Search-Expressions
        // . has a special meaning
        // * has more meanings

        // If it was none of the above treat as a literal hash key name
        // https://github.com/wwkimball/yamlpath/wiki/Segment:-Hash-Keys
        return yamlMappingNode.Children.Where(x =>
                x.Key is YamlScalarNode yamlScalarNode && yamlScalarNode.Value == yamlPathComponent)
            .Select(x => x.Value);
    }

    private static IEnumerable<YamlNode> PerformOperation(YamlMappingNode yamlMappingNode, string yamlPathComponent)
    {
        // Break break down the components
        // The Operation to perform
        // The element name to perform it on
        // The argument for the operation
        // If it should be inverted
        var (searchOperatorEnum, elementName, argument, invert) =
            ParseOperator(yamlPathComponent);

        // Perform the operation and update the outnodes
        return searchOperatorEnum switch
        {
            SearchOperatorEnum.Equals => GetNodesWithPredicate(yamlMappingNode, elementName, invert,
                yamlScalarNode => yamlScalarNode.Value == argument),
            SearchOperatorEnum.LessThan => GetNodesWithPredicate(yamlMappingNode, elementName, invert,
                yamlScalarNode =>
                    int.TryParse(yamlScalarNode.Value, out var value) && value < int.Parse(argument)),
            SearchOperatorEnum.GreaterThan => GetNodesWithPredicate(yamlMappingNode, elementName, invert,
                yamlScalarNode =>
                    int.TryParse(yamlScalarNode.Value, out var value) && value > int.Parse(argument)),
            SearchOperatorEnum.LessThanOrEqual => GetNodesWithPredicate(yamlMappingNode, elementName, invert,
                yamlScalarNode =>
                    int.TryParse(yamlScalarNode.Value, out var value) && value <= int.Parse(argument)),
            SearchOperatorEnum.GreaterThanOrEqual => GetNodesWithPredicate(yamlMappingNode, elementName, invert,
                yamlScalarNode =>
                    int.TryParse(yamlScalarNode.Value, out var value) && value >= int.Parse(argument)),
            SearchOperatorEnum.StartsWith => GetNodesWithPredicate(yamlMappingNode, elementName, invert,
                // Null checking is enforced before calling the predicate
                yamlScalarNode => yamlScalarNode.Value!.StartsWith(argument)),
            SearchOperatorEnum.EndsWith => GetNodesWithPredicate(yamlMappingNode, elementName, invert,
                // Null checking is enforced before calling the predicate
                yamlScalarNode => yamlScalarNode.Value!.EndsWith(argument)),
            SearchOperatorEnum.Contains => GetNodesWithPredicate(yamlMappingNode, elementName, invert,
                // Null checking is enforced before calling the predicate
                yamlScalarNode => yamlScalarNode.Value!.Contains(argument)),
            SearchOperatorEnum.Regex => GetNodesWithPredicate(yamlMappingNode, elementName, invert,
                // Null checking is enforced before calling the predicate
                yamlScalarNode => Regex.IsMatch(yamlScalarNode.Value!, argument)),
            SearchOperatorEnum.Invalid => throw new ArgumentOutOfRangeException(nameof(searchOperatorEnum)),
            _ => throw new ArgumentOutOfRangeException(nameof(searchOperatorEnum))
        };
    }

    /// <summary>
    ///     Get the selected nodes from the <see cref="YamlMappingNode" /> matching the element name and matching the predicate
    /// </summary>
    /// <param name="yamlMappingNode"></param>
    /// <param name="elementName"></param>
    /// <param name="invert">If the result of the predicate should be inverted</param>
    /// <param name="predicate">The predicate to test</param>
    /// <returns>Enumeration of YamlNodes that pass the predicate</returns>
    private static IEnumerable<YamlNode> GetNodesWithPredicate(YamlMappingNode yamlMappingNode, string elementName,
        bool invert, Func<YamlScalarNode, bool> predicate)
    {
        foreach (var child in yamlMappingNode.Children)
        {
            // The key must match the element name
            if (child.Key is YamlScalarNode { Value: { } } keyNode && keyNode.Value == elementName)
            {
                // Scalar nodes we can just return if they match the predicate
                if (child.Value is YamlScalarNode { Value: { } } valueNode)
                {
                    if (invert ? !predicate(valueNode) : predicate(valueNode))
                    {
                        yield return valueNode;
                    }
                }
                // Sequences we return the values of the sequence that match the predicate
                else if (child.Value is YamlSequenceNode yamlSequenceNode)
                {
                    foreach (var subChild in yamlSequenceNode.Children)
                    {
                        if (subChild is YamlScalarNode { Value: { } } subChildValueNode)
                        {
                            if (invert ? !predicate(subChildValueNode) : predicate(subChildValueNode))
                            {
                                yield return subChild;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Gets all of the Scalar nodes which are children or grandchildren etc of the given node
    /// </summary>
    /// <param name="yamlNode"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static IEnumerable<YamlNode> GetLeaves(YamlNode yamlNode)
    {
        return yamlNode switch
        {
            YamlScalarNode scalarNode => new[] { scalarNode },
            YamlSequenceNode sequenceNode => sequenceNode.Children.SelectMany(GetLeaves),
            YamlMappingNode mappingNode => mappingNode.Children.SelectMany(x => GetLeaves(x.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(yamlNode))
        };
    }

    /// <summary>
    ///     Parse the operator component into its pieces
    /// </summary>
    /// <param name="yamlPathComponent"></param>
    /// <returns></returns>
    private static ( SearchOperatorEnum operation, string elementName, string argument, bool invert)
        ParseOperator(string yamlPathComponent)
    {
        yamlPathComponent = yamlPathComponent.TrimStart('[').TrimEnd(']');
        var invert = false;
        if (yamlPathComponent[0] == '!')
        {
            yamlPathComponent = yamlPathComponent[1..];
            invert = !invert;
        }

        // First Check 2 length strings to greedily match
        foreach (var pair in StringToOperatorMapping.Where(x => x.Key.Length == 2))
        {
            var idx = yamlPathComponent.IndexOf(pair.Key, StringComparison.Ordinal);
            if (idx > -1)
            {
                if (yamlPathComponent[idx - 1] == '!')
                {
                    invert = !invert;
                    return (pair.Value, yamlPathComponent[..(idx - 1)].Trim(),
                        yamlPathComponent[(idx + pair.Key.Length)..].Trim(), invert);
                }

                return (pair.Value, yamlPathComponent[..idx].Trim(),
                    yamlPathComponent[(idx + pair.Key.Length)..].Trim(), invert);
            }
        }

        // No 2 length matches so we can check for 1 length matches
        foreach (var pair in StringToOperatorMapping.Where(x => x.Key.Length == 1))
        {
            var idx = yamlPathComponent.IndexOf(pair.Key, StringComparison.Ordinal);
            if (idx > -1)
            {
                if (yamlPathComponent[idx - 1] == '!')
                {
                    invert = !invert;
                    return (pair.Value, yamlPathComponent[..(idx - 1)].Trim(),
                        yamlPathComponent[(idx + pair.Key.Length)..].Trim(), invert);
                }

                return (pair.Value, yamlPathComponent[..idx].Trim(),
                    yamlPathComponent[(idx + pair.Key.Length)..].Trim(), invert);
            }
        }

        return (SearchOperatorEnum.Invalid, string.Empty, string.Empty, false);
    }

    /// <summary>
    ///     Select elements out of the mapping node based on a single path component
    /// </summary>
    /// <param name="yamlNode"></param>
    /// <param name="yamlPathComponent"></param>
    /// <returns></returns>
    private static List<YamlNode> SequenceNodeQuery(this YamlSequenceNode yamlNode, string yamlPathComponent)
    {
        var outNodes = new List<YamlNode>();
        // Wild Card
        if (yamlPathComponent == "*")
        {
            outNodes.AddRange(yamlNode.Children);
        }

        var expr = yamlPathComponent.Trim('[', ']');
        if (expr.Contains(':'))
        {
            // This can be one of three formats
            // 1. A number '0'
            // 2. A number in brackets '[0]'
            // 3. A range in brackets '[0:2]'
            // https://github.com/wwkimball/yamlpath/wiki/Segment:-Array-Elements
            // https://github.com/wwkimball/yamlpath/wiki/Segment:-Array-Slices
            var components = expr.Split(':');
            if (components.Length == 2 && int.TryParse(components[0], out var startRange) &&
                int.TryParse(components[1], out var endRange))
            {
                if (startRange < 0)
                    // The start might be a negative offset from the end
                {
                    startRange = yamlNode.Children.Count + startRange;
                }

                if (endRange < 0)
                {
                    endRange = yamlNode.Children.Count + endRange;
                }

                // Start range is inclusive so it must not be as much as the count
                if (startRange < 0 || startRange >= yamlNode.Children.Count)
                {
                    return outNodes;
                }

                // End is exclusive so it can be as large as the count
                if (endRange < 0 || endRange > yamlNode.Children.Count)
                {
                    return outNodes;
                }

                for (var i = startRange; i < endRange; i++)
                {
                    outNodes.Add(yamlNode[i]);
                }
            }
        }
        else
        {
            if (int.TryParse(expr, out var result))
            {
                if (yamlNode.Children.Count > result)
                {
                    outNodes.Add(yamlNode.Children[result]);
                }
            }
        }

        return outNodes;
    }

    /// <summary>
    ///     Gets all child nodes (and their children) recursively of the given node.
    /// </summary>
    /// <param name="currentNode">The node to explore</param>
    /// <returns>An enumeration of the child nodes</returns>
    /// <exception cref="ArgumentOutOfRangeException">If an unsupported type of <see cref="YamlNode" /> is provided</exception>
    private static IEnumerable<YamlNode> RecursiveGetAllNodes(YamlNode currentNode)
    {
        return currentNode switch
        {
            YamlMappingNode yamlMappingNode => yamlMappingNode.Children.Values.Union(
                yamlMappingNode.Children.SelectMany(x => RecursiveGetAllNodes(x.Value))),
            YamlSequenceNode yamlSequenceNode => yamlSequenceNode.Children.Union(
                yamlSequenceNode.Children.SelectMany(RecursiveGetAllNodes)),
            YamlScalarNode _ => Enumerable
                .Empty<YamlNode>(), // Scalar children were already added by the recursion in the sequence or mapping
            _ => throw new ArgumentOutOfRangeException(nameof(currentNode))
        };
    }

    /// <summary>
    ///     Break the provided <see cref="yamlPath" /> into components to iterate with
    /// </summary>
    /// <param name="yamlPath">The full YamlPath to break down</param>
    /// <returns>A list of the broken down path pieces</returns>
    private static List<string> GenerateNavigationElements(string yamlPath)
    {
        var pathComponents = new List<string>();
        // The separator can either be . or /, but if it is / the string must start with /
        var separator = yamlPath[0] == '/' ? '/' : '.';
        // Ignore separators in brackets
        var ignoreSeparator = false;

        // The start of the currently tracked token
        var startIndex = 0;
        for (var i = 0; i < yamlPath.Length; i++)
        {
            if (!ignoreSeparator)
            {
                // End of the token, add to components
                // Enter bracket mode, ignore separators until the next ]
                if (yamlPath[i] == '[')
                {
                    ignoreSeparator = true;
                    pathComponents.Add(yamlPath[startIndex..i]);
                    startIndex = i;
                }

                if (yamlPath[i] == separator)
                {
                    var isEscaped = false;
                    var innerItr = i;
                    // Walk backwards to count the number of escape characters to determine if this separator is escaped.
                    while (--innerItr >= 0 && yamlPath[innerItr] == EscapeCharacter)
                    {
                        isEscaped = !isEscaped;
                    }

                    if (!isEscaped)
                    {
                        pathComponents.Add(yamlPath[startIndex..i]);
                        startIndex = i + 1;
                    }
                }
            }
            else
            {
                if (yamlPath[i] == ']')
                {
                    ignoreSeparator = false;
                    pathComponents.Add(yamlPath[startIndex..(i + 1)]);
                    startIndex = i + 1;
                }
            }
        }

        pathComponents.Add(yamlPath[startIndex..]);
        pathComponents.RemoveAll(string.IsNullOrEmpty);
        pathComponents = pathComponents.Select(x => x.Replace($"\\{separator}", $"{separator}")).ToList();
        return pathComponents;
    }

    private static IEnumerable<YamlNode> AdvanceNode(YamlNode node, string pathComponent)
    {
        return node switch
        {
            YamlMappingNode yamlMappingNode => yamlMappingNode.MappingNodeQuery(pathComponent),
            YamlSequenceNode yamlSequenceNode => yamlSequenceNode.SequenceNodeQuery(pathComponent),
            YamlScalarNode _ => Enumerable.Empty<YamlNode>(), // No path inside a scalar
            _ => Enumerable.Empty<YamlNode>() // Nothing else is valid to continue
        };
    }

    /// <summary>
    ///     Enum for the types of search operation operators
    /// </summary>
    private enum SearchOperatorEnum
    {
        Invalid,
        Equals,
        LessThan,
        GreaterThan,
        LessThanOrEqual,
        GreaterThanOrEqual,
        StartsWith,
        EndsWith,
        Contains,
        Regex
    }
}