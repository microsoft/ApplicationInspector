// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// base for all command operation results
    /// </summary>
    public class Result
    {
        [JsonProperty(Order = 1, PropertyName = "appVersion")]
        public string? AppVersion { get; set; }
    }
}