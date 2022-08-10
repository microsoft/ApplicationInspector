// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions
{
    public class WithinClause : Clause
    {
        public WithinClause(Clause subClause, string? field = null) : base(Operation.Custom, field)
        {
            SubClause = subClause;
            CustomOperation = "Within";
        }

        public int After { get; set; }
        public int Before { get; set; }
        public bool OnlyBefore { get; set; }
        public bool OnlyAfter { get; set; }
        public bool SameFile { get; set; }
        public bool FindingOnly { get; set; }
        public bool SameLineOnly { get; set; }
        public bool FindingRegion { get; set; }
        public Clause SubClause { get; }
    }
}