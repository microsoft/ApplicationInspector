// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using System.Threading;

namespace Microsoft.ApplicationInspector.Commands;

/// <summary>
///     Used for metric results not included in match details reported other than count of instances for given tag i.e.
///     Metrics.[value].[value]
/// </summary>
public class MetricTagCounter
{
    private int _count;

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    [JsonPropertyName("count")]
    public int Count => _count;

    internal void IncrementCount(int amount = 1)
    {
        Interlocked.Add(ref _count, amount);
    }
}