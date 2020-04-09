// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Issue severity
    /// </summary>
    [Flags]
    public enum Severity
    {
        /// <summary>
        /// Critical issues
        /// </summary>
        Critical = 1,

        /// <summary>
        /// Important issues
        /// </summary>
        Important = 2,

        /// <summary>
        /// Moderate issues
        /// </summary>
        Moderate = 4,

        /// <summary>
        /// Best Practice
        /// </summary>
        BestPractice = 8,

        /// <summary>
        /// Issues that require manual review
        /// </summary>
        ManualReview = 16
    }

    /// <summary>
    /// Json Converter for Severity
    /// </summary>
    internal class SeverityConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Severity svr = (Severity)value;
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

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;
            enumString = enumString.Replace("-", "");
            return Enum.Parse(typeof(Severity), enumString, true);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}