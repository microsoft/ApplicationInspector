// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Pattern Type for search pattern
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PatternType
    {
        Regex,
        RegexWord,
        String,
        Substring
    }
}