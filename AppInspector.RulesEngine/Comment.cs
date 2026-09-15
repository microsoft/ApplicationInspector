// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     Comment class to hold information about comment for each language
/// </summary>
internal class Comment
{
    [JsonPropertyName("language")]
    public string[]? Languages { get; set; }

    [JsonPropertyName("inline")]
    public string? Inline { get; set; }

    /// <summary>
    ///     Additional single line comment markers for languages which support more than one style of inline comment.
    /// </summary>
    [JsonPropertyName("inlines")]
    public string[]? Inlines { get; set; }

    [JsonPropertyName("prefix")]
    public string? Prefix { get; set; }

    [JsonPropertyName("suffix")]
    public string? Suffix { get; set; }

    /// <summary>
    ///     Additional block comment marker pairs for languages which support more than one style of block comment.
    /// </summary>
    [JsonPropertyName("blocks")]
    public BlockComment[]? Blocks { get; set; }

    /// <summary>
    /// Set to true when these languages should always be considered comments (i.e. Plaintext files)
    /// </summary>
    [JsonPropertyName("always")] 
    public bool AlwaysCommented { get; set; }

    /// <summary>
    ///     Enumerates every single line comment marker specified, including the legacy single <see cref="Inline" /> value.
    /// </summary>
    internal IEnumerable<string> GetInlineComments()
    {
        if (!string.IsNullOrEmpty(Inline))
        {
            yield return Inline!;
        }

        foreach (var inline in Inlines ?? Array.Empty<string>())
            if (!string.IsNullOrEmpty(inline))
            {
                yield return inline!;
            }
    }

    /// <summary>
    ///     Enumerates every block comment marker pair specified, including the legacy single
    ///     <see cref="Prefix" />/<see cref="Suffix" /> pair.
    /// </summary>
    internal IEnumerable<(string Prefix, string Suffix)> GetBlockComments()
    {
        if (!string.IsNullOrEmpty(Prefix) && !string.IsNullOrEmpty(Suffix))
        {
            yield return (Prefix!, Suffix!);
        }

        foreach (var block in Blocks ?? Array.Empty<BlockComment>())
            if (!string.IsNullOrEmpty(block.Prefix) && !string.IsNullOrEmpty(block.Suffix))
            {
                yield return (block.Prefix!, block.Suffix!);
            }
    }
}
