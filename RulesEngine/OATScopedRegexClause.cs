// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    public class OATScopedRegexClause : Clause
    {
        public OATScopedRegexClause(PatternScope[] scopes, string? field = null) : base(Operation.Custom, field)
        {
            Scopes = scopes;
            CustomOperation = "ScopedRegex";
        }

        public PatternScope[] Scopes { get; }
    }
}