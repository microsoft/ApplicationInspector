// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions;

public class OatSubstringIndexClause : Clause
{
    public OatSubstringIndexClause(PatternScope[] scopes, string? field = null, bool useWordBoundaries = false,
        string[]? xPaths = null, string[]? jsonPaths = null, string[]? yamlPaths = null, Dictionary<string, string>? xPathNameSpaces = null) : base(Operation.Custom, field)
    {
        Scopes = scopes;
        CustomOperation = "SubstringIndex";
        UseWordBoundaries = useWordBoundaries;
        XPaths = xPaths;
        JsonPaths = jsonPaths;
        YmlPaths = yamlPaths;
        XPathNameSpaces = xPathNameSpaces ?? new();
    }

    public Dictionary<string, string> XPathNameSpaces { get; }

    public string[]? YmlPaths { get; }

    public string[]? JsonPaths { get; }

    public string[]? XPaths { get; }

    public PatternScope[] Scopes { get; }

    public bool UseWordBoundaries { get; }
}