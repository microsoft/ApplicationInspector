// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;
using System;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    ///     Issue severity
    /// </summary>
    [Flags]
    [JsonConverter(typeof(SeverityConverter))]
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

    /// <summary>
    ///     Json Converter for Severity
    /// </summary>
    internal class SeverityConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.Value is string enumString)
            {
                enumString = enumString.Replace("-", "");
                return Enum.Parse(typeof(Severity), enumString, true);
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is Severity svr)
            {
                string svrstr = svr.ToString().ToLower();

                switch (svr)
                {
                    case Severity.BestPractice:
                        svrstr = "best-practice";
                        break;

                    case Severity.ManualReview:
                        svrstr = "manual-review";
                        break;
                }

                writer.WriteValue(svrstr);
            }
        }
    }
}