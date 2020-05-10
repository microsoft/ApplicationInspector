// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Class to hold search pattern
    /// </summary>
    public class SearchPattern
    {
        private Confidence _confidence;
        private Regex _Expression;
        private string _Pattern;

        [JsonProperty(PropertyName = "pattern")]
        public string Pattern {
            get
            {
                return _Pattern;
            }
            set
            {
                _Pattern = value;
                // Reset expression so it gets regenerated on next request
                _Expression = null;
            }
        }

        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(PatternTypeConverter))]
        public PatternType PatternType { get; set; }

        [JsonProperty(PropertyName = "modifiers")]
        public string[] Modifiers { get; set; }

        [JsonProperty(PropertyName = "scopes")]
        public PatternScope[] Scopes { get; set; }

        [JsonProperty(PropertyName = "confidence")]
        public Confidence Confidence
        {
            get
            {
#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
                if (_confidence == null)//possible from serialiation
                {
#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
                    _confidence = Confidence.Medium;
                }

                return _confidence;
            }
            set => _confidence = value;
        }

        // Generate and cache the regex
        public Regex Expression {
            get
            {
                if (_Expression == null)
                {
                    RegexOptions reopt = RegexOptions.None;
                    if (Modifiers != null && Modifiers.Length > 0)
                    {
                        reopt |= Modifiers.Contains("i") ? RegexOptions.IgnoreCase : RegexOptions.None;
                        reopt |= Modifiers.Contains("m") ? RegexOptions.Multiline : RegexOptions.None;
                    }
                    reopt |= RegexOptions.Compiled;
                    _Expression = new Regex(Pattern, reopt);
                }
                return _Expression;
            }
        }

        public SearchPattern(string Pattern)
        {
            Confidence = Confidence.Medium;
        }
    }
}