// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace RulesEngine
{
    [JsonConverter(typeof(PatternScopeConverter))]
    public enum PatternScope
    {
        All,
        Code,
        Comment,
        Html
    }

    /// <summary>
    /// Json converter for Pattern Type
    /// </summary>
    class PatternScopeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            PatternScope svr = (PatternScope)value;
            string svrstr = svr.ToString().ToLower();

            writer.WriteValue(svrstr);
            writer.WriteValue(svr.ToString().ToLower());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;
            enumString = enumString.Replace("-", "");
            return Enum.Parse(typeof(PatternScope), enumString, true);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}
