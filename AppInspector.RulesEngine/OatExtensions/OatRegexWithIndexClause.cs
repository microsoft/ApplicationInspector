// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions
{
    public class OatRegexWithIndexClause : Clause
    {
        public OatRegexWithIndexClause(PatternScope[] scopes, string? field = null, string? xPath = null, string? jsonPath = null) : base(Operation.Custom, field)
        {
            Scopes = scopes;
            CustomOperation = "RegexWithIndex";
            XPath = xPath;
            JsonPath = jsonPath;
        }

        public string? JsonPath { get; }
        
        public string? XPath { get; }

        public PatternScope[] Scopes { get; }
    }
}