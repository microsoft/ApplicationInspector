// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Threading;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Used for metric results not included in match details reported other than count of instances for given tag i.e. Metrics.[value].[value]
    /// </summary>
    public class MetricTagCounter
    {
        [JsonProperty(PropertyName = "tag")]
        public string? Tag { get; set; }

        [JsonProperty(PropertyName = "count")]
        public int Count { get { return _count; } }

        private int _count = 0;

        internal void IncrementCount(int amount = 1)
        {
            Interlocked.Add(ref _count, amount);
        }
    }
}