// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;

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
    class PatternTypeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            PatternType svr = (PatternType)value;
            string svrstr = svr.ToString().ToLower();

            switch (svr)
            {
                case PatternType.RegexWord:
                    svrstr = "regex-word";
                    break;
            }
            writer.WriteValue(svrstr);
            writer.WriteValue(svr.ToString().ToLower());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;
            enumString = enumString.Replace("-", "");
            return Enum.Parse(typeof(PatternType), enumString, true);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}
