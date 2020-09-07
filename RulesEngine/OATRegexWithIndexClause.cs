// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    public class OATRegexWithIndexClause : Clause
    {
        public OATRegexWithIndexClause(PatternScope[] scopes, string? field = null) : base(Operation.Custom, field)
        {
            Scopes = scopes;
            CustomOperation = "RegexWithIndex";
        }

        public PatternScope[] Scopes { get; }
    }
}