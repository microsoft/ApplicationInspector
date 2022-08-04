// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions
{
    public class OatSubstringIndexClause : Clause
    {
        public OatSubstringIndexClause(PatternScope[] scopes, string? field = null, bool useWordBoundaries = false, string? xPath = null, string? jsonPath = null) : base(Operation.Custom, field)
        {
            Scopes = scopes;
            CustomOperation = "SubstringIndex";
            UseWordBoundaries = useWordBoundaries;
            XPath = xPath;
            JsonPath = jsonPath;
        }

        public string? JsonPath { get; }

        public string? XPath { get; }

        public PatternScope[] Scopes { get; }

        public bool UseWordBoundaries {get;}
    }
}