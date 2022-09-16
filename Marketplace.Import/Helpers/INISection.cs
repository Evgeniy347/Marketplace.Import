using System;
using System.Collections.Generic;

namespace Marketplace.Import.Helpers
{
    internal class INISection : IDictionaryParceValue
    {
        private readonly Dictionary<string, string> _values;

        public string this[string key]
        {
            get
            {
                TryGetValue(key, out string value);
                return value;
            }
            set => _values[key] = value;
        }

        public INISection(string name, Dictionary<string, string> values)
        {
            Name = name;
            _values = values;
        }

        public INISection(string name)
        {
            Name = name;
            _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; }

        public void AddValue(string key, string value)
        {
            _values[key] = value;
        }

        public bool TryGetValue(string key, out string value)
        {
            return _values.TryGetValue(key, out value);
        }
    }
}
