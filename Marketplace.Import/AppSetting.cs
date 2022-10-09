using Marketplace.Import.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.Import
{
    internal static class AppSetting
    {
        private static readonly object _lock = new object();
        private static volatile bool _init;

        private static string _RootFolder;
        public static string RootFolder
        {
            get
            {
                EnsureSettingsLoaded();
                return _RootFolder;
            }
        }

        private static string _FileFolderReport;
        public static string FileFolderReport
        {
            get
            {
                EnsureSettingsLoaded();
                return _FileFolderReport;
            }
        }

        private static string _CommonScript;
        public static string CommonScript
        {
            get
            {
                EnsureSettingsLoaded();
                return _CommonScript;
            }
        }


        private static string _LogsFolder;
        public static string LogsFolder
        {
            get
            {
                EnsureSettingsLoaded();
                return _LogsFolder;
            }
        }

        public static string FolderCache
        {
            get
            {
                string folder;

                if (!string.IsNullOrEmpty(RunScriptName))
                {
                    folder = $"Cache_Script_{RunScriptName}";
                    if (!string.IsNullOrEmpty(CurrentCredentialID))
                        folder = $"{folder}_{CurrentCredentialID}";

                    folder = Path.Combine(RootFolder, "Cache", folder);
                }
                else
                {
                    folder = Path.Combine(RootFolder, "Cache", "Default");
                }

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                return folder;
            }
        }

        private static PasswordManager _PasswordManager;
        public static PasswordManager PasswordManager
        {
            get
            {
                EnsureSettingsLoaded();
                return _PasswordManager;
            }
        }
        private static INIAdapterHelper _IniSettings;
        public static INIAdapterHelper IniSettings
        {
            get
            {
                EnsureSettingsLoaded();
                return _IniSettings;
            }
        }

        private static ScriptSetting[] _Scripts;
        public static ScriptSetting[] Scripts
        {
            get
            {
                EnsureSettingsLoaded();
                return _Scripts;
            }
        }

        private static string _RunScriptName;
        public static string RunScriptName => _RunScriptName;

        public static bool RunScript => !string.IsNullOrEmpty(RunScriptName);

        private static string _CurrentCredentialID;
        public static string CurrentCredentialID => _CurrentCredentialID;

        private static bool _ShowDevelop;
        public static bool ShowDevelop => _ShowDevelop;

        private static void EnsureSettingsLoaded()
        {
            if (!_init)
            {
                lock (_lock)
                {
                    if (!_init)
                    {
                        _IniSettings = new INIAdapterHelper("Default");
                        string fileScript = FindScriptFile();
                        _RootFolder = Path.GetDirectoryName(fileScript);
                        _IniSettings.OpenFile(fileScript);

                        _FileFolderReport = GetSettingPath("FolderReportFiles", "ReportFiles");
                        _LogsFolder = GetSettingPath("LogsFolder", "Logs");
                        _CommonScript = GetSettingPath("CommonScript");
                        string cryptDataFile = GetSettingPath("FilePassword", "passwordStorage.csv");
                        _PasswordManager = new PasswordManager(cryptDataFile);

                        List<INISection> sections = _IniSettings.GetSections("Script");
                        _Scripts = sections.Select(Convert).ToArray();

                        _init = true;
                    }
                }
            }
        }

        private static string FindScriptFile()
        {
            string sourcePath = Path.GetDirectoryName(typeof(AppSetting).Assembly.Location);
            string tempPath = Path.Combine(sourcePath, "Scripts.ini");

            bool isPublish = false;
            while (!File.Exists(tempPath))
            {
                if (isPublish)
                {
                    tempPath = Path.Combine(sourcePath, "Publish", "Scripts.ini");
                    isPublish = false;
                }
                else
                {
                    tempPath = Path.Combine(sourcePath, "Scripts.ini");
                    sourcePath = Path.GetDirectoryName(sourcePath);
                    isPublish = true;
                }
            }

            if (!File.Exists(tempPath))
                throw new Exception("Не удалось обнаружить файл скрипта");

            return tempPath;
        }

        private static string GetSettingPath(string key, string name = null)
        {
            _IniSettings.TryGetValue(key, out string path);
            if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(name))
                path = Path.Combine(_RootFolder, name);
            else
                path = GetFullPath(path);
            return path;
        }

        private static ScriptSetting Convert(INISection section)
        {
            ScriptSetting scriptSetting = new ScriptSetting()
            {
                Section = section,
                FileScript = section["FileScript"],
                Name = section["Name"],
                StartUrl = section["StartUrl"],
                ReportFile = section["ReportFile"],
                DefaultCredential = section["DefaultCredential"],
            };

            if (string.IsNullOrEmpty(scriptSetting.FileScript))
                throw new Exception("Не заполнен атрибут FileScript");

            if (string.IsNullOrEmpty(scriptSetting.Name))
                throw new Exception("Не заполнен атрибут Name");

            if (string.IsNullOrEmpty(scriptSetting.StartUrl))
                throw new Exception("Не заполнен атрибут StartUrl");

            Uri uri = new Uri(scriptSetting.StartUrl);

            scriptSetting.CheckHosts = section["CheckHosts"]?
               .Split(',')
               .Select(x => x.Trim().ToLower())?
               .Union(new string[] { uri.Host })
               .Where(x => !string.IsNullOrEmpty(x))?
               .Distinct()
               .ToArray() ?? new string[] { uri.Host };

            section.TryGetValue("WatchDog", out int wathcDog);
            scriptSetting.WatchDog = wathcDog;

            section.TryGetValue("Attempts", out int attempts);
            scriptSetting.Attempts = attempts;

            scriptSetting.FileScript = GetFullPath(scriptSetting.FileScript);
            scriptSetting.ReportFile = GetFullPath(scriptSetting.ReportFile);

            if (!File.Exists(scriptSetting.FileScript))
                throw new FileNotFoundException($"File '{scriptSetting.FileScript}' not found. Proprty 'FileScript'");


            return scriptSetting;
        }

        private static string GetFullPath(string path)
        {
            string result = path ?? string.Empty;
            if (!result.Contains(':'))
                result = Path.Combine(_RootFolder, result);
            return result;
        }

        private static INIAdapterHelper _argsParams;

        public static INIAdapterHelper ArgsParams => _argsParams;

        internal static void InitArgs(string[] argsParams)
        {
            INIAdapterHelper iniAdapter = _argsParams = new INIAdapterHelper();
            iniAdapter.OpenValue(argsParams);
            iniAdapter.TryGetValue("ShowDevelop", out _ShowDevelop);
            iniAdapter.TryGetValue("ScriptName", out _RunScriptName);
            iniAdapter.TryGetValue("CredentialID", out _CurrentCredentialID);
        }
          
        public static string ReplaceArgumentValue(string value)
        {
            string result = value;
            if (_argsParams != null)
            {
                foreach (INIValue valueIni in _argsParams.DefaulteSection.Values)
                    result = result.Replace($"{{{valueIni.Key}}}", valueIni.Value);
            }

            return result;
        }
    }
}

