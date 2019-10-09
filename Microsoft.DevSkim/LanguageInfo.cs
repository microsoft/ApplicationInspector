// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Content Type class
    /// </summary>
    class LanguageInfo
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "extensions")]
        public string[] Extensions { get; set; }
    }
}
