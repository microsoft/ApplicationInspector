// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.RulesEngine
{

    /// <summary>
    /// Analysis Issue
    /// </summary>
    public class ScanResult
    {
        Confidence _confidence;
        /// <summary>
        /// Creates new instance of Issue
        /// </summary>
        public ScanResult()
        {
            Rule = null;
            Boundary = new Boundary();
            StartLocation = new Location();
            IsSuppressionInfo = false;
            Confidence = Confidence.Medium;
        }

        public Confidence Confidence
        {
            get
            {
#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
                if (_confidence == null)//possible from serialiation
#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
                    _confidence = Confidence.Medium;

                return _confidence;
            }
            set
            {
                _confidence = value;
            }
        }

        /// <summary>
        /// Boundary of issue (index, length)
        /// </summary>
        public Boundary Boundary { get; set; }

        /// <summary>
        /// Location (line, column) where issue starts
        /// </summary>
        public Location StartLocation { get; set; }

        /// <summary>
        /// Location (line, column) where issue ends
        /// </summary>
        public Location EndLocation { get; set; }

        /// <summary>
        /// Matching rule
        /// </summary>
        public Rule Rule { get; set; }

        /// <summary>
        /// True if Issue refers to suppression information 
        /// </summary>
        public bool IsSuppressionInfo { get; set; }

        public SearchPattern PatternMatch { get; set; }
    }
}
