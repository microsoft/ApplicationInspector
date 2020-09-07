// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Pattern Type for search pattern
    /// </summary>
    public enum PatternType
    {
        Regex,
        RegexWord,
        String,
        Substring
    }

    /// <summary>
    /// Json converter for Pattern Type
    /// </summary>
    internal class PatternTypeConverter : JsonConverter
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
                return Enum.Parse(typeof(PatternType), enumString, true);
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is PatternType svr)
            {
                string svrstr = svr.ToString().ToLower(CultureInfo.InvariantCulture);

                switch (svr)
                {
                    case PatternType.RegexWord:
                        svrstr = "regex-word";
                        break;
                }
                writer.WriteValue(svrstr);
            }
        }
    }
}