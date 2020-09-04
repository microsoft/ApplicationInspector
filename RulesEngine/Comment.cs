// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Comment class to hold information about comment for each language
    /// </summary>
    internal class Comment
    {
        [JsonProperty(PropertyName = "language")]
        public string[]? Languages { get; set; }

        [JsonProperty(PropertyName = "inline")]
        public string? Inline { get; set; }

        [JsonProperty(PropertyName = "preffix")]
        public string? Prefix { get; set; }

        [JsonProperty(PropertyName = "suffix")]
        public string? Suffix { get; set; }
    }
}