using Newtonsoft.Json.Linq;
using Pipliz;
using System.Collections.Generic;

namespace Sign
{
    static class Extender
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary.TryGetValue(key, out TValue value))
                return value;

            return defaultValue;
        }


        public static JValue GetValueOrDefault<JValue>(this JObject jObject, string propertyName, out JValue value, JValue defaultValue)
        {
            value = jObject.GetAsOrDefault<JValue>(propertyName, defaultValue);

            return value;
        }

    }
}
