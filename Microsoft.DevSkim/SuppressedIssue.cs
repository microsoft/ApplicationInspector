// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Class holds information about suppressed issue
    /// </summary>
    public class SuppressedIssue
    {
        public Boundary Boundary { get; set; }
        public string ID { get; set; }
    }
}
