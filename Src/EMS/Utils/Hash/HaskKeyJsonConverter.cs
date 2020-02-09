using System;
using Newtonsoft.Json;
using AngryWasp.Helpers;

namespace EMS
{
    public class HashKey16JsonConverer : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(HashKey16);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            HashKey16 hk = ((string)reader.Value).FromByteHex();
            return hk;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            HashKey16 hk = (HashKey16)value;
            writer.WriteValue(hk.ToString());
        }
    }

    public class HashKey32JsonConverer : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(HashKey32);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            HashKey32 hk = ((string)reader.Value).FromByteHex();
            return hk;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            HashKey32 hk = (HashKey32)value;
            writer.WriteValue(hk.ToString());
        }
    }
}