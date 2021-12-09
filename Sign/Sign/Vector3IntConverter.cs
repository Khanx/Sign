using Newtonsoft.Json;
using System;
using Pipliz;

namespace Sign
{
    public class Vector3IntConverter : JsonConverter<Vector3Int>
    {

        public override Vector3Int ReadJson(JsonReader reader, Type objectType, Vector3Int existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return Vector3Int.Parse((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, Vector3Int value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
