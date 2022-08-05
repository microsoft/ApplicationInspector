// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions
{
    public class OatRegexWithIndexClause : Clause
    {
        public OatRegexWithIndexClause(PatternScope[] scopes, string? field = null, string[]? xPaths = null, string[]? jsonPaths = null) : base(Operation.Custom, field)
        {
            Scopes = scopes;
            CustomOperation = "RegexWithIndex";
            XPaths = xPaths;
            JsonPaths = jsonPaths;
        }

        public string[]? JsonPaths { get; }
        
        public string[]? XPaths { get; }

        public PatternScope[] Scopes { get; }
    }
}