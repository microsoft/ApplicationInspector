// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Code Fix Type
    /// </summary>
    public enum FixType
    {
        RegexReplace
    }


    /// <summary>
    /// Json Converter for FixType
    /// </summary>
    class FixTypeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            FixType svr = (FixType)value;
            string svrstr = svr.ToString().ToLower();

            switch (svr)
            {
                case FixType.RegexReplace:
                    svrstr = "regex-replace";
                    break;
            }
            writer.WriteValue(svrstr);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;
            enumString = enumString.Replace("-", "");
            return Enum.Parse(typeof(FixType), enumString, true);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
    
}
