// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;

    /// <summary>
    ///     Issue severity
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Severity
    {
        /// <summary>
        ///     Critial issues
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