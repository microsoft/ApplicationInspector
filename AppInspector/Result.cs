// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.ApplicationInspector.Commands
{
    using Newtonsoft.Json;

    /// <summary>
    /// base for all command operation results
    /// </summary>
    public class Result
    {
        [JsonProperty(Order = 1, PropertyName = "appVersion")]
        public string? AppVersion { get; set; }
    }
}