// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PatternScope
    {
        All,
        Code,
        Comment,
        Html
    }
}