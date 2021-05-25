// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;

namespace Microsoft.ApplicationInspector.RulesEngine
{
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