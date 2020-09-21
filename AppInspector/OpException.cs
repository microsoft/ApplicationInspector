// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Used to distinguish exceptions which are expected to have been safely written to log and console for CLI use
    /// to avoid duplication of error messages to better support both CLI and NuGet entry / exit points
    /// </summary>
    public class OpException : Exception
    {
        public OpException(string? msg) : base(msg)
        {
        }
    }
}