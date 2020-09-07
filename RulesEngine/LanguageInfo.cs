// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Content Type class
    /// </summary>
    public class LanguageInfo
    {
        public enum LangFileType { Code, Build };

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; } = "";

        [JsonProperty(PropertyName = "extensions")]
        public string[]? Extensions { get; set; }

        [JsonProperty(PropertyName = "type")]
        public LangFileType Type { get; set; } = LangFileType.Code;
    }

   
}