using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Confidence { Unspecified = 0, Low = 1, Medium = 2, High = 4 }
}