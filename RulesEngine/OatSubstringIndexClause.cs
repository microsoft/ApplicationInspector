// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using Microsoft.CST.OAT;

    public class OATSubstringIndexClause : Clause
    {
        public OATSubstringIndexClause(PatternScope[] scopes, string? field = null, bool useWordBoundaries = false) : base(Operation.Custom, field)
        {
            Scopes = scopes;
            CustomOperation = "SubstringIndex";
            UseWordBoundaries = useWordBoundaries;
        }

        public PatternScope[] Scopes { get; }

        public bool UseWordBoundaries {get;}
    }
}