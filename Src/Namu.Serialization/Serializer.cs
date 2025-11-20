using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Namu.Serialization
{
    public static class Serializer
    {
        public static string Serialize(object obj)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.All,
                ObjectCreationHandling = ObjectCreationHandling.Reuse,
                Converters = { new SerializableObjectWriter() }
            };

            string json = JsonConvert.SerializeObject(obj, settings);

            return json;
        }

        public static T Deserialize<T>(string json, ReadOnlyDictionary<string, string>? typeNames = null)
        {
            var converter = new SerializableObjectReader() { TypeNames = typeNames };

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.All,
                ObjectCreationHandling = ObjectCreationHandling.Reuse,
                Converters = { converter }
            };

            var data = JsonConvert.DeserializeObject<T>(json, settings)!;

            return data;
        }

        internal static string GetTypeName(Type type)
        {
            return $"{type.FullName}, {type.Assembly.GetName().Name}";
        }
    }
}
