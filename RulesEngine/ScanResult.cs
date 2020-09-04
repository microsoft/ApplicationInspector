// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Analysis Issue
    /// </summary>
    public class ScanResult
    {
        private Confidence _confidence =  RulesEngine.Confidence.Medium;

          public Confidence Confidence
        {
            get
            {
                return _confidence;
            }
            set => _confidence = value;
        }

        public string Excerpt { get; set; } = "";

        public string Sample { get; set; } = "";

        /// <summary>
        /// Boundary of issue (index, length)
        /// </summary>
        public Boundary Boundary { get; set; } = new Boundary();

        /// <summary>
        /// Location (line, column) where issue starts
        /// </summary>
        public Location StartLocation { get; set; } = new Location();

        /// <summary>
        /// Location (line, column) where issue ends
        /// </summary>
        public Location EndLocation { get; set; } = new Location();

        /// <summary>
        /// Matching rule
        /// </summary>
        public Rule Rule { get; set; } = new Rule();

        public SearchPattern PatternMatch { get; set; } = new SearchPattern();
    }
}