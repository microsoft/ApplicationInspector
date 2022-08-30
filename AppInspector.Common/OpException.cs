// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.ApplicationInspector.Common;

/// <summary>
///     Used to distinguish exceptions which are expected to have been safely written to log and console for CLI use
///     to avoid duplication of error messages to better support both CLI and NuGet entry / exit points
/// </summary>
[ExcludeFromCodeCoverage]
public class OpException : Exception
{
    public OpException(string? msg) : base(msg)
    {
    }

    public OpException()
    {
    }

    public OpException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}