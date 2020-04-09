// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    public class SearchCondition
    {
        [JsonProperty(PropertyName = "pattern")]
        public SearchPattern Pattern { get; set; }

        [JsonProperty(PropertyName = "search_in")]
        public string SearchIn { get; set; }

        [JsonProperty(PropertyName = "negate_finding")]
        public bool NegateFinding { get; set; }
    }
}