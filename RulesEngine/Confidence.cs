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
    class ConfidenceConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Confidence svr = (Confidence)value;
            string svrstr = svr.ToString().ToLower();

            writer.WriteValue(svrstr);

        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;
            return Enum.Parse(typeof(Confidence), enumString, true);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}
