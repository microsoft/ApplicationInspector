using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace AppInspector.YamlPath
{
    public enum SearchOperatorEnum
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
        Regex,
        Invert
    }
    
    public static class YamlPathExtensions
    {
        private static SearchOperatorEnum StringToOperatorEnum(string searchOperatorString)
        {
            return searchOperatorString switch
            {
                "=" => SearchOperatorEnum.Equals,
                "==" => SearchOperatorEnum.Equals,
                "<" => SearchOperatorEnum.LessThan,
                ">" => SearchOperatorEnum.GreaterThan,
                "<=" => SearchOperatorEnum.LessThanOrEqual,
                ">=" => SearchOperatorEnum.GreaterThanOrEqual,
                "^" => SearchOperatorEnum.StartsWith,
                "$" => SearchOperatorEnum.EndsWith,
                "%" => SearchOperatorEnum.Contains,
                "=~" => SearchOperatorEnum.Regex,
                "!" => SearchOperatorEnum.Invert,
                _ => SearchOperatorEnum.Invalid
            };
        }
        
        /// <summary>
        /// Select elements out of the mapping node based on a single path component
        /// </summary>
        /// <param name="yamlNode"></param>
        /// <param name="yamlPathComponent"></param>
        /// <returns></returns>
        private static List<YamlNode> MappingNodeQuery(this YamlMappingNode yamlNode, string yamlPathComponent)
        {
            List<YamlNode> outNodes = new List<YamlNode>();
            var expr = yamlPathComponent.Trim(new[] { '[', ']' });
            // Maps support slices
            // https://github.com/wwkimball/yamlpath/wiki/Segment:-Hash-Slices
            if (expr.Contains(':'))
            {
                var components = expr.Split(':');
                var started = false;

                for (int i = 0; i < yamlNode.Children.Count; i++)
                {
                    if (started)
                    {
                        outNodes.Add(yamlNode.Children[i].Value);
                        // If we find the end, we return
                        if (yamlNode.Children[i].Key is YamlScalarNode yamlScalarKey && (yamlScalarKey.Value?.Equals(components[1]) ?? false))
                        {
                            return outNodes;
                        }
                    }
                    else
                    {
                        // Find the start key
                        if (yamlNode.Children[i].Key is YamlScalarNode yamlScalarKey && (yamlScalarKey.Value?.Equals(components[0]) ?? false))
                        {
                            started = true;
                            outNodes.Add(yamlNode.Children[i].Value);
                        }
                    }
                }

                // If we get here we did not find the end, so the selection is empty
                outNodes.Clear();
            }
            else
            {
                // An expression
                // https://github.com/wwkimball/yamlpath/wiki/Search-Expressions
                if (yamlPathComponent.StartsWith('[') && yamlPathComponent.EndsWith(']'))
                {
                    (SearchOperatorEnum searchOperatorEnum, string elementName, string argument, bool invert) =
                        ParseOperator(yamlPathComponent);
                    switch (searchOperatorEnum)
                    {
                        case SearchOperatorEnum.Equals:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode keyNode 
                                         && keyNode.Value == elementName 
                                         && x.Value is YamlScalarNode { Value: { } } valueNode 
                                         && (invert ? valueNode.Value != argument : valueNode.Value == argument)))
                            {
                                outNodes.Add(child.Value);
                            }
                            foreach (var child in yamlNode.Children.Where(x =>
                                             x.Key is YamlScalarNode keyNode 
                                             && keyNode.Value == elementName)
                                         .SelectMany(x => x.Value is YamlSequenceNode yamlSequenceNode ?
                                             yamlSequenceNode.Children.Where(y => y is YamlScalarNode { Value: { } } childAsScalar 
                                                 && (invert ? childAsScalar.Value != argument : childAsScalar.Value == argument)) : Array.Empty<YamlNode>()))
                            {
                                outNodes.Add(child);
                            }
                            break;
                        case SearchOperatorEnum.LessThan:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode keyNode 
                                         && keyNode.Value == elementName 
                                         && x.Value is YamlScalarNode valueNode 
                                         && int.TryParse(valueNode.Value, out int value) 
                                         && (invert ? !(value < int.Parse(argument)) : value < int.Parse(argument))))
                            {
                                outNodes.Add(child.Value);
                            }                            
                            foreach (var child in yamlNode.Children.Where(x =>
                                             x.Key is YamlScalarNode keyNode 
                                             && keyNode.Value == elementName && x.Value is YamlSequenceNode)
                                         .SelectMany(x => x.Value is YamlSequenceNode yamlSequenceNode ?
                                             yamlSequenceNode.Children.Where(y => y is YamlScalarNode childAsScalar 
                                                 && int.TryParse(childAsScalar.Value, out int value) 
                                                 && (invert ? !(value < int.Parse(argument)) : value < int.Parse(argument))) : Array.Empty<YamlNode>()))
                            {
                                outNodes.Add(child);
                            }
                            break;
                        case SearchOperatorEnum.GreaterThan:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode keyNode 
                                         && keyNode.Value == elementName 
                                         && x.Value is YamlScalarNode valueNode 
                                         && int.TryParse(valueNode.Value, out int value) 
                                         && (invert ? !(value > int.Parse(argument)) : value > int.Parse(argument))))
                            {
                                outNodes.Add(child.Value);
                            }                            
                            foreach (var child in yamlNode.Children.Where(x =>
                                             x.Key is YamlScalarNode keyNode 
                                             && keyNode.Value == elementName && x.Value is YamlSequenceNode)
                                         .SelectMany(x => x.Value is YamlSequenceNode yamlSequenceNode ?
                                             yamlSequenceNode.Children.Where(y => y is YamlScalarNode childAsScalar 
                                                 && int.TryParse(childAsScalar.Value, out int value) 
                                                 && (invert ? !(value > int.Parse(argument)) : value > int.Parse(argument))) : Array.Empty<YamlNode>()))
                            {
                                outNodes.Add(child);
                            }
                            break;                        
                        case SearchOperatorEnum.LessThanOrEqual:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode keyNode 
                                         && keyNode.Value == elementName 
                                         && x.Value is YamlScalarNode valueNode 
                                         && int.TryParse(valueNode.Value, out int value) 
                                         && (invert ? !(value <= int.Parse(argument)) : value <= int.Parse(argument))))
                            {
                                outNodes.Add(child.Value);
                            }                            
                            foreach (var child in yamlNode.Children.Where(x =>
                                             x.Key is YamlScalarNode keyNode 
                                             && keyNode.Value == elementName && x.Value is YamlSequenceNode)
                                         .SelectMany(x => x.Value is YamlSequenceNode yamlSequenceNode ?
                                             yamlSequenceNode.Children.Where(y => y is YamlScalarNode childAsScalar 
                                                 && int.TryParse(childAsScalar.Value, out int value) 
                                                 && (invert ? !(value <= int.Parse(argument)) : value <= int.Parse(argument))) : Array.Empty<YamlNode>()))
                            {
                                outNodes.Add(child);
                            }
                            break;
                        case SearchOperatorEnum.GreaterThanOrEqual:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode keyNode 
                                         && keyNode.Value == elementName 
                                         && x.Value is YamlScalarNode valueNode 
                                         && int.TryParse(valueNode.Value, out int value) 
                                         && (invert ? !(value >= int.Parse(argument)) : value >= int.Parse(argument))))
                            {
                                outNodes.Add(child.Value);
                            }                            
                            foreach (var child in yamlNode.Children.Where(x =>
                                             x.Key is YamlScalarNode keyNode 
                                             && keyNode.Value == elementName && x.Value is YamlSequenceNode)
                                         .SelectMany(x => x.Value is YamlSequenceNode yamlSequenceNode ?
                                             yamlSequenceNode.Children.Where(y => y is YamlScalarNode childAsScalar 
                                                 && int.TryParse(childAsScalar.Value, out int value) 
                                                 && (invert ? !(value >= int.Parse(argument)) : value >= int.Parse(argument))) : Array.Empty<YamlNode>()))
                            {
                                outNodes.Add(child);
                            }
                            break;
                        case SearchOperatorEnum.StartsWith:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode keyNode 
                                         && keyNode.Value == elementName 
                                         && x.Value is YamlScalarNode { Value: { } } valueNode 
                                         && (invert ? !valueNode.Value.StartsWith(argument) : valueNode.Value.StartsWith(argument))))
                            {
                                outNodes.Add(child.Value);
                            }
                            foreach (var child in yamlNode.Children.Where(x =>
                                             x.Key is YamlScalarNode keyNode 
                                             && keyNode.Value == elementName)
                                         .SelectMany(x => x.Value is YamlSequenceNode yamlSequenceNode ?
                                             yamlSequenceNode.Children.Where(y => y is YamlScalarNode { Value: { } } childAsScalar 
                                                 && (invert ? !childAsScalar.Value.StartsWith(argument) : childAsScalar.Value.StartsWith(argument))) : Array.Empty<YamlNode>()))
                            {
                                outNodes.Add(child);
                            }
                            break;
                        case SearchOperatorEnum.EndsWith:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode keyNode 
                                         && keyNode.Value == elementName 
                                         && x.Value is YamlScalarNode { Value: { } } valueNode 
                                         && (invert ? !valueNode.Value.EndsWith(argument) : valueNode.Value.EndsWith(argument))))
                            {
                                outNodes.Add(child.Value);
                            }
                            foreach (var child in yamlNode.Children.Where(x =>
                                             x.Key is YamlScalarNode keyNode 
                                             && keyNode.Value == elementName)
                                         .SelectMany(x => x.Value is YamlSequenceNode yamlSequenceNode ?
                                             yamlSequenceNode.Children.Where(y => y is YamlScalarNode { Value: { } } childAsScalar 
                                                 && (invert ? !childAsScalar.Value.EndsWith(argument) : childAsScalar.Value.EndsWith(argument))) : Array.Empty<YamlNode>()))
                            {
                                outNodes.Add(child);
                            }
                            break;
                        case SearchOperatorEnum.Contains:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode keyNode 
                                         && keyNode.Value == elementName 
                                         && x.Value is YamlScalarNode { Value: { } } valueNode 
                                         && (invert ? !valueNode.Value.Contains(argument) : valueNode.Value.Contains(argument))))
                            {
                                outNodes.Add(child.Value);
                            }
                            foreach (var child in yamlNode.Children.Where(x =>
                                             x.Key is YamlScalarNode keyNode 
                                             && keyNode.Value == elementName)
                                         .SelectMany(x => x.Value is YamlSequenceNode yamlSequenceNode ?
                                             yamlSequenceNode.Children.Where(y => y is YamlScalarNode { Value: { } } childAsScalar 
                                                 && (invert ? !childAsScalar.Value.StartsWith(argument) : childAsScalar.Value.StartsWith(argument))) : Array.Empty<YamlNode>()))
                            {
                                outNodes.Add(child);
                            }
                            break;
                        case SearchOperatorEnum.Regex:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode keyNode 
                                         && keyNode.Value == elementName 
                                         && x.Value is YamlScalarNode { Value: { } } valueNode 
                                         && (invert ? !Regex.IsMatch(valueNode.Value, argument) : Regex.IsMatch(valueNode.Value, argument))))
                            {
                                outNodes.Add(child.Value);
                            }
                            foreach (var child in yamlNode.Children.Where(x =>
                                             x.Key is YamlScalarNode keyNode 
                                             && keyNode.Value == elementName)
                                         .SelectMany(x => x.Value is YamlSequenceNode yamlSequenceNode ?
                                             yamlSequenceNode.Children.Where(y => y is YamlScalarNode { Value: { } } childAsScalar 
                                                 && (invert ? !Regex.IsMatch(childAsScalar.Value, argument) : Regex.IsMatch(childAsScalar.Value, argument))) : Array.Empty<YamlNode>()))
                            {
                                outNodes.Add(child);
                            }
                            break;
                        case SearchOperatorEnum.Invert: // TODO
                        case SearchOperatorEnum.Invalid:
                        default:
                            outNodes.Clear();
                            break;                    
                    }
                }
                else
                {
                    // Wild Cards https://github.com/wwkimball/yamlpath/wiki/Wildcard-Segments
                    // TODO: ** recursive wildcard
                    if (yamlPathComponent == "*")
                    {
                        outNodes.AddRange(yamlNode.Children.Values);
                    }
                    // https://github.com/wwkimball/yamlpath/wiki/Segment:-Hash-Keys
                    // TODO: Quoted dot named keys
                    // TODO: Escaped name keys
                    foreach (var child in yamlNode.Children.Where(x =>
                                 x.Key is YamlScalarNode yamlScalarNode && yamlScalarNode.Value == yamlPathComponent))
                    {
                        outNodes.Add(child.Value);
                    }
                }
            }
            
            return outNodes;
        }

        private static Dictionary<string, SearchOperatorEnum> StringToOperatorMapping =
            new Dictionary<string, SearchOperatorEnum>()
            {
                {"==", SearchOperatorEnum.Equals},
                {"=", SearchOperatorEnum.Equals},
                {"=~", SearchOperatorEnum.Regex},
                {"<=", SearchOperatorEnum.LessThanOrEqual},
                {">=", SearchOperatorEnum.GreaterThanOrEqual},
                {"<", SearchOperatorEnum.LessThan},
                {">", SearchOperatorEnum.GreaterThan},
                {"^", SearchOperatorEnum.StartsWith},
                {"$", SearchOperatorEnum.EndsWith},
                {"%", SearchOperatorEnum.Contains}
            };
        
        private static ( SearchOperatorEnum operation, string elementName, string argument, bool invert)
            ParseOperator(string yamlPathComponent)
        {
            yamlPathComponent = yamlPathComponent.TrimStart('[').TrimEnd(']');
            bool invert = false;
            if (yamlPathComponent[0] == '!')
            {
                yamlPathComponent = yamlPathComponent[1..];
                invert = !invert;
            }
            // First Check 2 length strings
            foreach(var pair in StringToOperatorMapping.Where(x => x.Key.Length == 2))
            {
                var idx = -1;
                idx = yamlPathComponent.IndexOf(pair.Key, StringComparison.Ordinal);
                if (idx > -1)
                {
                    if (yamlPathComponent[idx - 1] == '!')
                    {
                        invert = !invert;
                        return (pair.Value, yamlPathComponent[..(idx-1)].Trim(), yamlPathComponent[(idx + pair.Key.Length)..].Trim(), invert);
                    }
                    else
                    {
                        return (pair.Value, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + pair.Key.Length)..].Trim(), invert);
                    }
                }
            }
            foreach(var pair in StringToOperatorMapping.Where(x => x.Key.Length == 1))
            {
                var idx = -1;
                idx = yamlPathComponent.IndexOf(pair.Key, StringComparison.Ordinal);
                if (idx > -1)
                {
                    if (yamlPathComponent[idx - 1] == '!')
                    {
                        invert = !invert;
                        return (pair.Value, yamlPathComponent[..(idx-1)].Trim(), yamlPathComponent[(idx + pair.Key.Length)..].Trim(), invert);
                    }
                    else
                    {
                        return (pair.Value, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + pair.Key.Length)..].Trim(), invert);
                    }
                }
            }

            return (SearchOperatorEnum.Invalid, string.Empty, string.Empty, false);
        }
        
        /// <summary>
        /// Select elements out of the mapping node based on a single path component
        /// </summary>
        /// <param name="yamlNode"></param>
        /// <param name="yamlPathComponent"></param>
        /// <returns></returns>
        private static List<YamlNode> SequenceNodeQuery(this YamlSequenceNode yamlNode, string yamlPathComponent)
        {
            List<YamlNode> outNodes = new List<YamlNode>();
            // Wild Card
            // TODO: recursive wildcard
            if (yamlPathComponent == "*")
            {
                outNodes.AddRange(yamlNode.Children);
            }
            
            var expr = yamlPathComponent.Trim(new[] { '[', ']' });
            if (expr.Contains(':'))
            {
                // This can be one of three formats
                // 1. A number '0'
                // 2. A number in brackets '[0]'
                // 3. A range in brackets '[0:2]'
                // https://github.com/wwkimball/yamlpath/wiki/Segment:-Array-Elements
                // https://github.com/wwkimball/yamlpath/wiki/Segment:-Array-Slices
                var components = expr.Split(':');
                if (components.Length == 2 && int.TryParse(components[0], out int startRange) &&
                    int.TryParse(components[1], out int endRange))
                {
                    if (startRange < 0)
                    {
                        // The start might be a negative offset from the end
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

                    for (int i = startRange; i < endRange; i++)
                    {
                        outNodes.Add(yamlNode[i]);
                    }
                }
            }
            else
            {
                if (int.TryParse(expr, out int result))
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
        /// Get all the <see cref="YamlNode"/> that match the provided yamlPath
        /// </summary>
        /// <param name="yamlNode">The YamlMappingNode to operate on</param>
        /// <param name="yamlPath">The YamlPath query to use</param>
        /// <returns>An <see cref="List{YamlNode}"/> of the matching nodes</returns>
        public static List<YamlNode> YamlPathQuery(this YamlNode yamlNode, string yamlPath)
        {
            // TODO: validate query
            
            List<string> navigationElements = GenerateNavigationElements(yamlPath);

            List<YamlNode> currentNodes = new List<YamlNode>(){yamlNode};
        
            // Iteratively walk using the navigation elements
            foreach (var navigationElement in navigationElements)
            {
                // The list of nodes we can be in after parsing the next nav element
                List<YamlNode> nextNodes = new List<YamlNode>();
                foreach (var currentNode in currentNodes)
                {
                    // Advance the current node to all possible next nodes with the navigation element
                    nextNodes.AddRange(AdvanceNode(currentNode, navigationElement));
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

        private static List<string> GenerateNavigationElements(string yamlPath)
        {
            List<string> pathComponents = new List<string>();
            // The separator can either be . or /, but if / it must start with /
            char separator = yamlPath[0] == '/' ? '/' : '.';
            bool ignoreSeparator = false;
            int startIndex = 0;
            for (int i = 0; i < yamlPath.Length; i++)
            {
                if (!ignoreSeparator)
                {
                    // Ignore separators in brackets
                    if (yamlPath[i] == '[')
                    {
                        ignoreSeparator = true;
                        pathComponents.Add(yamlPath[startIndex..i]);
                        startIndex = i;
                    }

                    if (yamlPath[i] == separator)
                    {
                        bool isEscaped = false;
                        int innerItr = i;
                        while (--innerItr >= 0 && yamlPath[innerItr] == '\\')
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
            => node switch
            {
                YamlMappingNode yamlMappingNode => yamlMappingNode.MappingNodeQuery(pathComponent),
                YamlSequenceNode yamlSequenceNode => yamlSequenceNode.SequenceNodeQuery(pathComponent),
                YamlScalarNode _ => Enumerable.Empty<YamlNode>(), // No path inside a scalar
                _ => Enumerable.Empty<YamlNode>() // Nothing else is valid to continue
            };
    }
}
