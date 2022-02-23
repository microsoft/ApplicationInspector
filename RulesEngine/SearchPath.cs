// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Class to hold search path
    /// </summary>
    public class SearchPath
    {
        [JsonProperty(PropertyName = "path")]
        public string? Path { get; set; }
    }
}