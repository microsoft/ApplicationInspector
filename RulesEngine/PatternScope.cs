// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PatternScope
    {
        All,
        Code,
        Comment,
        Html
    }
}