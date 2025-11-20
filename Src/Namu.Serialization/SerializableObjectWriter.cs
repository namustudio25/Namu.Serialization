using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Namu.Serialization
{
    internal class SerializableObjectWriter : JsonConverter
    {
        private readonly Dictionary<object, string> _objectToId = new Dictionary<object, string>();
        private int _nextId = 1;

        public override bool CanWrite => true;
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return typeof(ISerializableObject).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (!(value is ISerializableObject))
            {
                throw new ArgumentException("It has to be ISerializableObject", nameof(value));
            }

            // already done, $ref
            if (_objectToId.TryGetValue(value, out var existingId))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("$ref");
                writer.WriteValue(existingId);
                writer.WriteEndObject();
                return;
            }

            // register new object
            string newId = "_" + (_nextId++).ToString();
            _objectToId[value] = newId;

            var type = value.GetType();

            writer.WriteStartObject();

            writer.WritePropertyName("$id");
            writer.WriteValue(newId);

            writer.WritePropertyName("$type");

            // with no versioning
            //writer.WriteValue(type.AssemblyQualifiedName);
            writer.WriteValue(Serializer.GetTypeName(type));

            var info = new SerializableObjectInfo();
            ((ISerializableObject)value).GetObjectData(info);

            foreach (var kv in info.Values)
            {
                writer.WritePropertyName(kv.Key);
                serializer.Serialize(writer, kv.Value);
            }

            writer.WriteEndObject();
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new InvalidOperationException("This is the Writer");
        }
    }
}
