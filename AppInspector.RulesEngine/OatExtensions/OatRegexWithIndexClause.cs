// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions;

public class OatRegexWithIndexClause : Clause
{
    public OatRegexWithIndexClause(PatternScope[] scopes, string? field = null, string[]? xPaths = null,
        string[]? jsonPaths = null, string[]? ymlPaths = null, Dictionary<string, string>? xPathNameSpaces = null) : base(Operation.Custom, field)
    {
        Scopes = scopes;
        CustomOperation = "RegexWithIndex";
        XPaths = xPaths;
        JsonPaths = jsonPaths;
        YmlPaths = ymlPaths;
        XPathNameSpaces = xPathNameSpaces ?? new();
    }

    public string[]? JsonPaths { get; }

    public string[]? XPaths { get; }

    public Dictionary<string, string> XPathNameSpaces { get; }

    /// <summary>
    ///     Index of the <see cref="SearchPattern" /> in the originating rule that this clause was built from.
    ///     Used to report which pattern matched; -1 when the clause is a condition subclause.
    /// </summary>
    public int PatternIndex { get; set; } = -1;

    public PatternScope[] Scopes { get; }
    public string[]? YmlPaths { get; }
}