using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Windows.Forms.LinkLabel;

namespace Marketplace.Import.Helpers
{
    public class INIAdapterHelper : IDictionaryParceValue
    {
        private readonly INISection _defaulteSection;
        private readonly Dictionary<string, List<INISection>> _sections;
        private List<INISection> _allSection = new List<INISection>();

        public INISection DefaulteSection => _defaulteSection;

        public INIAdapterHelper(string defaulteSectionName = null)
        {
            _sections = new Dictionary<string, List<INISection>>();
            _defaulteSection = new INISection(defaulteSectionName);
            _allSection.Add(_defaulteSection);
        }

        public INISection CreateSection(string key)
        {
            INISection section = new INISection(key);
            if (!_sections.TryGetValue(key, out List<INISection> sections))
                _sections[key] = sections = new List<INISection>();
            _allSection.Add(section);
            sections.Add(section);
            return section;
        }

        public INISection GetSection(string key, bool throwNotFound = false)
        {
            INISection section = GetSections(key).FirstOrDefault();
            if (section == null && throwNotFound)
                throw new Exception($"not found section name '{key}'");

            return section;
        }

        public List<INISection> GetSections(string key)
        {
            if (_sections.TryGetValue(key, out List<INISection> sections))
                return sections;

            return new List<INISection>();
        }

        public void OpenFile(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            OpenValue(lines);
        }

        public bool TryGetValue(string key, out string value) =>
            _defaulteSection.TryGetValue(key, out value);

        public void Save(string fileIni)
        {
            string[] lines = Render();
            File.WriteAllLines(fileIni, lines);
        }

        public string[] Render()
        {
            string[] lines = _allSection.SelectMany(x => x.Render()).ToArray();
            return lines;
        }

        internal void OpenValue(string[] lines)
        {
            INISection currentSection = _defaulteSection;

            foreach (string line in lines)
            {
                string formatLine = line.Trim('\t', ' ');

                if (string.IsNullOrEmpty(formatLine) || formatLine.StartsWith("#"))
                {
                    currentSection.AddLine(formatLine);
                    continue;
                }

                if (formatLine[0] == '[' && formatLine[formatLine.Length - 1] == ']')
                {
                    formatLine = formatLine.TrimStart('[').TrimEnd(']');
                    if (_defaulteSection.Name == formatLine)
                    {
                        currentSection = _defaulteSection;
                    }
                    else
                    {
                        currentSection = CreateSection(formatLine);
                    }
                }
                else
                {
                    int keyIndexEnd = formatLine.IndexOf('=');
                    if (keyIndexEnd == -1)
                    {
                        currentSection.AddValue(formatLine);
                    }
                    else
                    {
                        string key = formatLine.Substring(0, keyIndexEnd).Trim('\t', ' ');
                        string value = formatLine.Substring(keyIndexEnd + 1).Trim('\t', ' ');
                        currentSection.AddValue(key, value);
                    }
                }
            }
        }
    }

    public class INIValue
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool OnlyKey { get; internal set; }
        public string Line { get; internal set; }
        public bool IsLine { get; internal set; }

        public override string ToString()
        {
            if (IsLine)
                return Line;

            if (OnlyKey)
                return Key;

            return $"{Key}={Value}";
        }
    }

    public class INISection : IDictionaryParceValue
    {
        private readonly List<INIValue> _values;

        public List<INIValue> Values => _values;

        public string this[string key]
        {
            get
            {
                TryGetValue(key, out string value);
                return value;
            }
            set
            {
                INIValue iniValue = _values
                    .Where(x => !x.IsLine)
                    .FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));

                if (iniValue == null)
                    AddValue(key, value);
                else
                {
                    iniValue.Value = value;
                    iniValue.OnlyKey = false;
                }
            }
        }

        public INISection(string name)
        {
            Name = name;
            _values = new List<INIValue>();
        }

        public string Name { get; }

        public IEnumerable<string> Render()
        {
            if (Name != null)
                yield return $"[{Name}]";

            foreach (INIValue value in _values)
                yield return value.ToString();
        }

        public INIValue AddLine(string line = null)
        {
            INIValue result = new INIValue() { Line = line, IsLine = true };
            _values.Add(result);
            return result;
        }

        public INIValue AddValue(string key)
        {
            INIValue result = new INIValue() { Key = key, OnlyKey = true };
            _values.Add(result);
            return result;
        }

        public INIValue AddValue(string key, string value)
        {
            INIValue result = new INIValue() { Key = key, Value = value };
            _values.Add(result);
            return result;
        }

        public bool TryGetValue(string key, out string value)
        {
            INIValue iniValue = _values.Where(x => !x.IsLine)
                .FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            value = iniValue?.Value;
            return iniValue != null;
        }

        public int Remove(string key) =>
            _values.RemoveAll(x => !x.IsLine && x.Key == key);
    }

    public interface IDictionaryParceValue
    {
        bool TryGetValue(string key, out string value);
    }

    internal static class DictionaryParceValueExtensions
    {
        public static bool TryGetValue(this IDictionaryParceValue dic, string key, out int value) =>
             TryGetValue(dic, key, out value, int.Parse);

        public static bool TryGetValue(this IDictionaryParceValue dic, string key, out bool value) =>
             TryGetValue(dic, key, out value, bool.Parse);

        public static bool TryGetValue<T>(this IDictionaryParceValue dic, string key, out T value, Func<string, T> parcer)
        {
            if (!dic.TryGetValue(key, out string valueStr) || string.IsNullOrEmpty(valueStr))
            {
                value = default;
                return false;
            }

            try
            {
                value = parcer(valueStr);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при чтении значения '{}'", ex); ;
            }
        }
    }
}
