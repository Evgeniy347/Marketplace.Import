﻿using Marketplace.Import.Helpers;
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
        private static bool _init;

        public static readonly string RootFolder = Path.GetDirectoryName(Path.GetDirectoryName(typeof(AppSetting).Assembly.Location));

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

        private static PasswordManager _PasswordManager;
        public static PasswordManager PasswordManager
        {
            get
            {
                EnsureSettingsLoaded();
                return _PasswordManager;
            }
        }
        private static INIReaderHelper _IniSettings;
        public static INIReaderHelper IniSettings
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

        public static string RunScriptName { get; set; }

        public static bool RunScript => !string.IsNullOrEmpty(RunScriptName);

        private static void EnsureSettingsLoaded()
        {
            if (!_init)
            {
                lock (_lock)
                {
                    if (!_init)
                    {
                        _IniSettings = new INIReaderHelper();
                        string fileScript = Path.Combine(RootFolder, "Scripts.ini");
                        _IniSettings.OpenFile(fileScript);

                        _FileFolderReport = GetSettingPath("FolderReportFiles", "ReportFiles");
                        _LogsFolder = GetSettingPath("LogsFolder", "Logs");
                        _CommonScript = GetSettingPath("CommonScript");
                        string cryptDataFile = GetSettingPath("FilePassword", "cryptData.csv");
                        _PasswordManager = new PasswordManager(cryptDataFile);

                        List<INISection> sections = _IniSettings.GetSections("Script");
                        _Scripts = sections.Select(Convert).ToArray();

                        _init = true;
                    }
                }
            }
        }

        private static string GetSettingPath(string key, string name = null)
        {
            _IniSettings.TryGetValue(key, out string path);
            if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(name))
                path = Path.Combine(RootFolder, name);
            else
                path = GetFullPath(path);
            return path;
        }

        private static ScriptSetting Convert(INISection section)
        {
            ScriptSetting scriptSetting = new ScriptSetting()
            {
                FileScript = section["FileScript"],
                Name = section["Name"],
                StartUrl = section["StartUrl"],
                ReportFile = section["ReportFile"],
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
                result = Path.Combine(RootFolder, result);
            return result;
        }
    }
}