// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PatternScope
    {
        All,
        Code,
        Comment,
        Html
    }
}