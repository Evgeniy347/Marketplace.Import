using System;
using System.Collections.Generic;
using System.IO;

namespace Marketplace.Import.Helpers
{
    internal class INIReaderHelper : IDictionaryParceValue
    {
        private string _filePath;
        private bool _open;
        public readonly INISection _defaulteSection;
        private readonly Dictionary<string, List<INISection>> _sections;
        public INIReaderHelper()
        {
            _sections = new Dictionary<string, List<INISection>>();
            _defaulteSection = new INISection("Default");
            _sections[_defaulteSection.Name] = new List<INISection>() { _defaulteSection };
        }

        public List<INISection> GetSections(string key)
        {
            if (_sections.TryGetValue(key, out List<INISection> sections))
                return sections;

            return new List<INISection>();
        }

        public void OpenFile(string filePath)
        {
            if (_open)
                throw new Exception("Файл уже открыт");

            _filePath = filePath;

            string[] lines = File.ReadAllLines(_filePath);

            INISection currentSection = _defaulteSection;

            foreach (string line in lines)
            {
                string formatLine = line.Trim('\t', ' ');

                if (string.IsNullOrEmpty(formatLine) || formatLine.StartsWith("#"))
                    continue;

                if (formatLine.Length > 0 && formatLine[0] == '[' && formatLine[formatLine.Length - 1] == ']')
                {
                    formatLine = formatLine.TrimStart('[').TrimEnd(']');
                    if (_defaulteSection.Name == formatLine)
                    {
                        currentSection = _defaulteSection;
                    }
                    else
                    {
                        currentSection = new INISection(formatLine);
                        if (!_sections.TryGetValue(formatLine, out List<INISection> sections))
                            _sections[formatLine] = sections = new List<INISection>();
                        sections.Add(currentSection);
                    }
                }
                else
                {
                    int keyIndexEnd = formatLine.IndexOf('=');
                    if (keyIndexEnd == -1)
                    {
                        currentSection[formatLine] = string.Empty;
                    }
                    else
                    {
                        string key = formatLine.Substring(0, keyIndexEnd).Trim('\t', ' ');
                        string value = formatLine.Substring(keyIndexEnd + 1).Trim('\t', ' ');
                        currentSection[key] = value;
                    }
                }
            }

            _open = true;
        }

        public bool TryGetValue(string key, out string value) =>
            _defaulteSection.TryGetValue(key, out value);

    }
}
