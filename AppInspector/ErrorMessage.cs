// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.AppInspector.CLI
{
    class ErrorMessage
    {
        public string File { get; set; }
        public string Path { get; set; }
        public string Message { get; set; }
        public string RuleID { get; set; }
        public bool Warning { get; set; }
    }
}
