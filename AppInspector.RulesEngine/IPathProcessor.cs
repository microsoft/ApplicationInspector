// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.ApplicationInspector.RulesEngine.Processors;

/// <summary>
/// Interface for path-based query processors
/// </summary>
internal interface IPathProcessor
{
    IEnumerable<(string value, Boundary location)> GetMatches(string path, Dictionary<string, string>? parameters = null);
}