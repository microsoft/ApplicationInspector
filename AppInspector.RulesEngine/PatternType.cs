// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        Substring,
        // Included for legacy DevSkim rule support
        [Obsolete("Use the RegexWord value with no hyphen instead.")]
        [EnumMember(Value = "regex-word")]
        RegexWordWithHyphen = 1
    }
}