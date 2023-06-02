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

    public PatternScope[] Scopes { get; }
    public string[]? YmlPaths { get; }
}