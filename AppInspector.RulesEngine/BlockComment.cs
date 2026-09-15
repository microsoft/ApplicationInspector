// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     A pair of markers which delimit a block (multi-line) comment.
/// </summary>
internal class BlockComment
{
    [JsonPropertyName("prefix")]
    public string? Prefix { get; set; }

    [JsonPropertyName("suffix")]
    public string? Suffix { get; set; }
}
