// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.Commands;

/// <summary>
///     Used for metric results not included in match details reported other than count of instances for given tag i.e.
///     Metrics.[value].[value]
/// </summary>
public class MetricTagCounter
{
    private int _count;

    [JsonProperty(PropertyName = "tag")] public string? Tag { get; set; }

    [JsonProperty(PropertyName = "count")] public int Count => _count;

    internal void IncrementCount(int amount = 1)
    {
        Interlocked.Add(ref _count, amount);
    }
}