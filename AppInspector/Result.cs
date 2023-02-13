// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.Commands;

/// <summary>
///     base for all command operation results
/// </summary>
public class Result
{
    [JsonPropertyName("appVersion")] // Order 1
    public string? AppVersion { get; set; }
}