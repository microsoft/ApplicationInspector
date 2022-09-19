using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
                if (yamlPathComponent.StartsWith('[') && yamlPathComponent.EndsWith(']'))
                {
                    (SearchOperatorEnum searchOperatorEnum, string elementName, string argument) =
                        ParseOperatorPathComponents(yamlPathComponent);
                    switch (searchOperatorEnum)
                    {
                        case SearchOperatorEnum.Equals:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode ycn && ycn.Value == elementName && x.Value is YamlScalarNode ycn2 && ycn2.Value == argument))
                            {
                                outNodes.Add(child.Value);
                            }
                            break;
                        case SearchOperatorEnum.LessThan:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode ycn && ycn.Value == elementName && x.Value is YamlScalarNode ycn2 && int.TryParse(ycn2.Value, out int value) && value < int.Parse(argument)))
                            {
                                outNodes.Add(child.Value);
                            }
                            break;
                        case SearchOperatorEnum.GreaterThan:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode ycn && ycn.Value == elementName && x.Value is YamlScalarNode ycn2 && int.TryParse(ycn2.Value, out int value) && value  > int.Parse(argument)))
                            {
                                outNodes.Add(child.Value);
                            }
                            break;                        
                        case SearchOperatorEnum.LessThanOrEqual:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode ycn && ycn.Value == elementName && x.Value is YamlScalarNode ycn2 && int.TryParse(ycn2.Value, out int value) && value  <= int.Parse(argument)))
                            {
                                outNodes.Add(child.Value);
                            }
                            break;
                        case SearchOperatorEnum.GreaterThanOrEqual:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode ycn && ycn.Value == elementName && x.Value is YamlScalarNode ycn2 && int.TryParse(ycn2.Value, out int value) && value  >= int.Parse(argument)))
                            {
                                outNodes.Add(child.Value);
                            }
                            break;
                        case SearchOperatorEnum.StartsWith:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode ycn && ycn.Value == elementName && x.Value is YamlScalarNode ycn2 && ycn2.Value.StartsWith(argument)))
                            {
                                outNodes.Add(child.Value);
                            }
                            break;
                        case SearchOperatorEnum.EndsWith:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode ycn && ycn.Value == elementName && x.Value is YamlScalarNode ycn2 && ycn2.Value.EndsWith(argument)))
                            {
                                outNodes.Add(child.Value);
                            }
                            break;
                        case SearchOperatorEnum.Contains:
                            foreach (var child in yamlNode.Children.Where(x =>
                                         x.Key is YamlScalarNode ycn && ycn.Value == elementName && x.Value is YamlScalarNode ycn2 && ycn2.Value.Contains(argument)))
                            {
                                outNodes.Add(child.Value);
                            }
                            break;
                        case SearchOperatorEnum.Regex: // TODO
                        case SearchOperatorEnum.Invert: // TODO
                        case SearchOperatorEnum.Invalid:
                        default:
                            outNodes.Clear();
                            break;                    }
                }
                else
                {
                    // https://github.com/wwkimball/yamlpath/wiki/Segment:-Hash-Keys
                    // TODO: Quoted dot named keys
                    // TODO: Escaped name keys
                    // Wild Cards https://github.com/wwkimball/yamlpath/wiki/Wildcard-Segments
                    // TODO: ** recursive wildcard
                    if (yamlPathComponent == "*")
                    {
                        outNodes.AddRange(yamlNode.Children.Values);
                    }
                    // Direct key lookup
                    foreach (var child in yamlNode.Children.Where(x =>
                                 x.Key is YamlScalarNode ycn && ycn.Value == yamlPathComponent))
                    {
                        outNodes.Add(child.Value);
                    }
                }
            }
            
            return outNodes;
        }

        private static (SearchOperatorEnum searchOperatorEnum, string elementName, string argument) ParseOperatorPathComponents(string yamlPathComponent)
        {
            yamlPathComponent = yamlPathComponent.Trim(new[] { '[', ']' });
            var idx = -1;
            idx = yamlPathComponent.IndexOf("==", StringComparison.Ordinal);
            if (idx > -1)
            {
                return (SearchOperatorEnum.Equals, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + 2)..].Trim());
            }
            idx = yamlPathComponent.IndexOf("=~", StringComparison.Ordinal);
            if (idx > -1)
            {
                return (SearchOperatorEnum.Regex, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + 2)..].Trim());
            }
            idx = yamlPathComponent.IndexOf("<=", StringComparison.Ordinal);
            if (idx > -1)
            {
                return (SearchOperatorEnum.LessThanOrEqual, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + 2)..].Trim());
            }
            idx = yamlPathComponent.IndexOf(">=", StringComparison.Ordinal);
            if (idx > -1)
            {
                return (SearchOperatorEnum.GreaterThanOrEqual, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + 2)..].Trim());
            }
            idx = yamlPathComponent.IndexOf("=", StringComparison.Ordinal);
            if (idx > -1)
            {
                return (SearchOperatorEnum.Equals, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + 1)..].Trim());
            }
            idx = yamlPathComponent.IndexOf("<", StringComparison.Ordinal);
            if (idx > -1)
            {
                return (SearchOperatorEnum.LessThan, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + 1)..].Trim());
            }
            idx = yamlPathComponent.IndexOf(">", StringComparison.Ordinal);
            if (idx > -1)
            {
                return (SearchOperatorEnum.GreaterThan, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + 1)..].Trim());
            }
            idx = yamlPathComponent.IndexOf("^", StringComparison.Ordinal);
            if (idx > -1)
            {
                return (SearchOperatorEnum.StartsWith, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + 1)..].Trim());
            }
            idx = yamlPathComponent.IndexOf("$", StringComparison.Ordinal);
            if (idx > -1)
            {
                return (SearchOperatorEnum.EndsWith, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + 1)..].Trim());
            }
            idx = yamlPathComponent.IndexOf("%", StringComparison.Ordinal);
            if (idx > -1)
            {
                return (SearchOperatorEnum.Contains, yamlPathComponent[..idx].Trim(), yamlPathComponent[(idx + 1)..].Trim());
            }
            
            return (SearchOperatorEnum.Invalid, string.Empty, string.Empty);
        }

        /// <summary>
        /// Select elements out of the mapping node based on a single path component
        /// </summary>
        /// <param name="yamlNode"></param>
        /// <param name="yamlPathComponent"></param>
        /// <returns></returns>
        private static IEnumerable<YamlNode> SequenceNodeQuery(this YamlSequenceNode yamlNode, string yamlPathComponent)
        {
            // This can be one of three formats
            // 1. A number '0'
            // 2. A number in brackets '[0]'
            // 3. A range in brackets '[0:2]'
            // 4. A search Expression // TODO
            // https://github.com/wwkimball/yamlpath/wiki/Segment:-Array-Elements
            // https://github.com/wwkimball/yamlpath/wiki/Segment:-Array-Slices
            // https://github.com/wwkimball/yamlpath/wiki/Search-Expressions
            var expr = yamlPathComponent.Trim(new[] { '[', ']' });
            if (expr.Contains(':'))
            {
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
                        yield break;
                    }
                    // End is exclusive so it can be as large as the count
                    if (endRange < 0 || endRange > yamlNode.Children.Count)
                    {
                        yield break;
                    }
                    for(int i = startRange; i < endRange; i++)
                    {
                        yield return yamlNode[i];
                    }
                }
            }
            else
            {
                if (int.TryParse(expr, out int result))
                {
                    if (yamlNode.Children.Count > result)
                    {
                        yield return yamlNode[result];
                    }
                }
            }
        }
        
        /// <summary>
        /// Get all the <see cref="YamlNode"/> that match the provided yamlPath
        /// </summary>
        /// <param name="yamlNode">The YamlMappingNode to operate on</param>
        /// <param name="yamlPath">The YamlPath query to use</param>
        /// <returns>An enumeration of the matching <see cref="YamlNode"/></returns>
        public static List<YamlNode> YamlPathQuery(this YamlNode yamlNode, string yamlPath)
        {
            // TODO: validate query

            // The separator can either be . or /, but if / it must start with /
            char separator = yamlPath[0] == '/' ? '/' : '.';
            // The first split is empty with '/', so we remove it
            var navigationElements = ((separator == '/') ? 
                    yamlPath.Split(separator)[1..] : yamlPath.Split(separator))
                // A slice can be used directly after an element, rather than after a separator
                // In such a case, '[' will be at an index greater than 0
                .SelectMany(x => x.IndexOf('[') > 0 ? 
                    // We need to split apart around the slice operator. Since the split removes the start we need to remove the end
                    x.Split('[').Select(y => y.EndsWith(']') ? $"[{y}" : y) : new []{x}).ToArray();
            
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
