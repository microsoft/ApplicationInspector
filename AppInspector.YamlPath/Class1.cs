using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using YamlDotNet.RepresentationModel;

namespace AppInspector.YamlPath
{
    public static class YamlPathExtensions
    {
        /// <summary>
        /// Select elements out of the mapping node based on a single path component
        /// </summary>
        /// <param name="yamlNode"></param>
        /// <param name="yamlPathComponent"></param>
        /// <returns></returns>
        private static IEnumerable<YamlNode> MappingNodeQuery(this YamlMappingNode yamlNode, string yamlPathComponent)
        {
            foreach (var child in yamlNode.Children.Where(x =>
                         x.Key is YamlScalarNode ycn && ycn.Value == yamlPathComponent))
            {
                yield return child.Value;
            }
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
        public static List<YamlNode> YamlPathQuery(this YamlMappingNode yamlNode, string yamlPath)
        {
            // validate query
        
            // split query
        
            // The separator can either be . or /, but if / it must start with /
            char separator = yamlPath[0] == '/' ? '/' : '.';
            string[] navigationElements = yamlPath.Split(separator)[1..];
            List<YamlNode> currentNodes = new List<YamlNode>(){yamlNode};
        
            foreach (var navigationElement in navigationElements)
            {
                List<YamlNode> nextNodes = new List<YamlNode>();
                foreach (var currentNode in currentNodes)
                {
                    nextNodes.AddRange(currentNode switch
                    {
                        YamlMappingNode yamlMappingNode => yamlMappingNode.MappingNodeQuery(navigationElement),
                        YamlSequenceNode yamlSequenceNode => yamlSequenceNode.SequenceNodeQuery(navigationElement),
                        YamlScalarNode yamlScalarNode => Enumerable.Empty<YamlNode>(), // No path inside a scalar
                        _ => Enumerable.Empty<YamlNode>() // Nothing else is valid to continue
                    });
                }

                // Nothing matched the next sequence, so stop enumeration.
                if (!nextNodes.Any())
                {
                    return new List<YamlNode>();
                }
                currentNodes = nextNodes;
            }

            return currentNodes;
        }
    }
}
