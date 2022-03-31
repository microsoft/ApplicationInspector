// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Comment class to hold information about comment for each language
    /// </summary>
    internal class Comment
    {
        [JsonPropertyName("language")]
        public string[]? Languages { get; set; }

        [JsonPropertyName("inline")]
        public string? Inline { get; set; }

        [JsonPropertyName("prefix")]
        public string? Prefix { get; set; }

        [JsonPropertyName("suffix")]
        public string? Suffix { get; set; }
    }
}