// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Class holds information about suppressed issue
    /// </summary>
    public class SuppressedMatch
    {
        public Boundary Boundary { get; set; }
        public string ID { get; set; }
    }
}