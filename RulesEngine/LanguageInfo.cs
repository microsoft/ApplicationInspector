// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Content Type class
    /// </summary>
    public class LanguageInfo
    {
        public enum LangFileType { Code, Build };

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("extensions")]
        public string[]? Extensions { get; set; }

        [JsonPropertyName("file-names")]
        public string[]? FileNames { get; set; }
        
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LangFileType Type { get; set; } = LangFileType.Code;
    }
}