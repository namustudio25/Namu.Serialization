using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Namu.Serialization
{
    internal class SerializableObjectReader : JsonConverter
    {
        private readonly Dictionary<string, object> _idToObject = new Dictionary<string, object>();

        private readonly List<(object Obj, string MemberName, string RefId)> _waitingRefIds = new List<(object Obj, string MemberName, string RefId)>();
        private readonly List<(IList List, string RefId)> _waitingRefIdsForList = new List<(IList List, string RefId)>();

        private readonly List<(ISerializableObject Object, SerializableObjectInfo Info)> _objectInfos = new List<(ISerializableObject Object, SerializableObjectInfo Info)>();


        public override bool CanWrite => false;
        public override bool CanRead => true;

        public ReadOnlyDictionary<string, string>? TypeNames { get; set; }


        public override bool CanConvert(Type objectType)
        {
            return typeof(ISerializableObject).IsAssignableFrom(objectType)
                || typeof(IList).IsAssignableFrom(objectType);

        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("This is the Reader");
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var jo = Newtonsoft.Json.Linq.JObject.Load(reader);

            var data = ReadValue(jo, serializer, out _);

            SetValues();

            return data;
        }

        private void SetValues()
        {
            foreach (var item in _waitingRefIds)
            {
                SetValue(item.Obj, item.MemberName, item.RefId);
            }

            foreach (var item in _waitingRefIdsForList)
            {
                _idToObject.TryGetValue(item.RefId, out var value);
                item.List.Add(value);
            }

            foreach (var item in _objectInfos)
            {
                item.Object.SetObjectData(item.Info);
            }
        }

        private void SetValue(object obj, string memberName, string refId)
        {
            if (_idToObject.TryGetValue(refId, out var value) && value != null)
            {
                var member = obj.GetType().GetMember(memberName,
                    System.Reflection.MemberTypes.Property | System.Reflection.MemberTypes.Field,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .First();

                if (member is PropertyInfo propertyInfo)
                {
                    propertyInfo.SetValue(obj, value);
                }
                else if (member is FieldInfo fieldInfo)
                {
                    fieldInfo.SetValue(obj, value);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private object? ReadValue(Newtonsoft.Json.Linq.JToken token, JsonSerializer serializer, out string? notReadyRefId)
        {
            if (token.Type != JTokenType.Object)
            {
                notReadyRefId = null;
                return token.ToObject<object?>(serializer);
            }

            var jo = (Newtonsoft.Json.Linq.JObject)token;

            if (jo.TryGetValue("$ref", out var refToken))
            {
                string refId = refToken!.ToString();
                if (_idToObject.ContainsKey(refId))
                {
                    notReadyRefId = null;
                    return _idToObject[refId];
                }

                notReadyRefId = refId;
                return null;//hasn't prepared yet.
            }

            string id = jo["$id"]!.ToString();
            string typeName = jo["$type"]!.ToString();

            if (TypeNames != null
                && TypeNames.ContainsKey(typeName))
            {
                typeName = TypeNames[typeName];
            }

            var type = Type.GetType(typeName) ?? throw new InvalidOperationException($"Type not found: {typeName}");

            // $values
            if (jo.TryGetValue("$values", out var valuesToken))
            {
                //var elementType = type.IsArray ? type.GetElementType()! : type.GenericTypeArguments[0];
                //var elementType = type.GenericTypeArguments[0];
                var tempList = Activator.CreateInstance(type)!;

                if (tempList is IList list)
                {
                    _idToObject[id] = list;

                    foreach (var itemToken in valuesToken)
                    {
                        var temp = ReadValue(itemToken, serializer, out var notReadyRefId2);

                        if (notReadyRefId2 == null)
                        {
                            list.Add(temp);
                        }
                        else
                        {
                            _waitingRefIdsForList.Add((list, notReadyRefId2));
                        }
                    }

                    //_idToObject[id] = list;

                    notReadyRefId = null;
                    return list;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }


            {
                // search default empty constructor
                ConstructorInfo? ctor = type.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    binder: null,
                    types: Array.Empty<Type>(),
                    modifiers: null
                );

                if (ctor == null)
                    throw new InvalidProgramException("needs default empty constructor");

                var obj = ctor.Invoke(null);
                _idToObject[id] = obj;

                var valuesObj = new SerializableObjectInfo();
                List<(string MemberName, string RefId)> notReadyInfos = new List<(string MemberName, string RefId)>();

                foreach (var kv in jo.Children<JProperty>())
                {
                    if (kv.Name.StartsWith("$"))
                        continue;

                    valuesObj.AddValue(kv.Name, ReadValue(kv.Value, serializer, out var notReadyRefId2));

                    if (notReadyRefId2 != null)
                    {
                        notReadyInfos.Add((kv.Name, notReadyRefId2));
                    }
                }

                _objectInfos.Add(((ISerializableObject)obj, valuesObj));

                foreach (var item in notReadyInfos)
                {
                    _waitingRefIds.Add((obj, item.MemberName, item.RefId));
                }

                notReadyRefId = null;
                return obj;
            }

        }
    }
}
