using Newtonsoft.Json;
using System;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    [Flags]
    [JsonConverter(typeof(ConfidenceConverter))]
    public enum Confidence { Low = 1, Medium = 2, High = 4 }

    /// <summary>
    /// Json converter for Pattern Type
    /// </summary>
    internal class ConfidenceConverter : JsonConverter
    {
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.Value is string enumString)
            {
                return Enum.Parse(typeof(Confidence), enumString, true);
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is Confidence svr)
            {
                string svrstr = svr.ToString().ToLower();
                writer.WriteValue(svrstr);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}