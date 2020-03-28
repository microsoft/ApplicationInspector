// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Class to hold search pattern
    /// </summary>
    public class SearchPattern
    {
        Confidence _confidence;

        [JsonProperty(PropertyName = "pattern")]
        public string Pattern { get; set; }

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
                if (_confidence == null)//possible from serialiation
                    _confidence = Confidence.Medium;

                return _confidence;
            }
            set
            {
                _confidence = value;
            }
        }

        public SearchPattern()
        {
            Confidence = Confidence.Medium;
        }
    }


}
