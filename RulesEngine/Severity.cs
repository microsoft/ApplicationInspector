// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using System;

    /// <summary>
    ///     Issue severity
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Severity
    {
        /// <summary>
        ///     Has not been specified
        /// </summary>
        Unspecified = 0,
        /// <summary>
        ///     Critical issues
        /// </summary>
        Critical = 1,

        /// <summary>
        ///     Important issues
        /// </summary>
        Important = 2,

        /// <summary>
        ///     Moderate issues
        /// </summary>
        Moderate = 4,

        /// <summary>
        ///     Best Practice
        /// </summary>
        BestPractice = 8,

        /// <summary>
        ///     Issues that require manual review
        /// </summary>
        ManualReview = 16
    }
}