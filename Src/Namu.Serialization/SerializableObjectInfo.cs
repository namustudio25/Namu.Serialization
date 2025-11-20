using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Namu.Serialization
{
    public class SerializableObjectInfo
    {
        private readonly Dictionary<string, object?> _values = new Dictionary<string, object?>();

        public IReadOnlyDictionary<string, object?> Values => _values;

        public T GetValue<T>(string name)
        {
            if (!_values.TryGetValue(name, out var obj)) return default!;

            if (obj is T t) return t;

            return (T)Convert.ChangeType(obj, typeof(T))!;
        }

        public void AddValue(string name, object? value)
        {
            Debug.Assert(!_values.ContainsKey(name));

            _values[name] = value;
        }
    }
}
