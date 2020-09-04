// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    public class SearchCondition
    {
        [JsonProperty(PropertyName = "negate_finding")]
        public bool NegateFinding { get; set; }

        [JsonProperty(PropertyName = "pattern")]
        public SearchPattern? Pattern { get; set; }

        [JsonProperty(PropertyName = "search_in")]
        public string? SearchIn { get; set; }
    }
}