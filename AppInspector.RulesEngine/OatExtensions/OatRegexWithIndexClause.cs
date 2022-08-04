// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions
{
    public class OatRegexWithIndexClause : Clause
    {
        public OatRegexWithIndexClause(PatternScope[] scopes, string? field = null, string? structuredPath = null) : base(Operation.Custom, field)
        {
            Scopes = scopes;
            CustomOperation = "RegexWithIndex";
            StructuredPath = structuredPath;
        }

        public string? StructuredPath { get; }

        public PatternScope[] Scopes { get; }
    }
}