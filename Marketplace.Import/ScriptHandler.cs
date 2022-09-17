using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Marketplace.Import
{
    internal class ScriptHandler
    {
        private readonly ChromiumWebBrowser _browser;
        private string _jsonContextValue;
        private string _jsonContextKey = "MPS_Context";
        private ScriptSetting _currentScript;

        private WatchDog _WatchDog;
        public bool WatchDogEnable { get; set; }
        private int _countAttempts;
        public ScriptHandler(ChromiumWebBrowser browser)
        {
            _browser = browser;
            //Событие изменения статуса рендера страницы
            browser.LoadingStateChanged += OnLoadingStateChanged;

            //Событие console.log
            browser.ConsoleMessage += OnConsoleMessage;
        }

        public ScriptSetting CurrentScript => _currentScript;

        private void OnConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            if (_currentScript == null)
                return;

            if (!string.IsNullOrEmpty(e.Message))
            {
                if (e.Message.StartsWith(_jsonContextKey))
                {
                    _jsonContextValue = e.Message;
                    _WatchDog?.Reset();
                }
                else if (e.Message.StartsWith("FileReportUrl:"))
                {
                    string url = e.Message.Replace("FileReportUrl:", "");
                    _WatchDog?.Dispose();
                    _browser.StartDownload(url);
                }
                else if (e.Message.StartsWith("WatchDogReset"))
                {
                    _WatchDog?.Reset();
                }
            }
        }

        private void OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (_currentScript == null)
                return;

            //Дожидаемся загрузки страницы
            bool isLoading = !e.CanReload;
            if (!isLoading)
            {
                //Проверяем url страницы
                if (_browser.Address.CheckHostMask(_currentScript.CheckHosts))
                {
                    //Добавляем контекст операции
                    AddScriptContext();

                    //Добавляем скрипт 
                    AddScript();
                }
            }
        }

        public void Stop()
        {
            _currentScript = null;
            _WatchDog?.Dispose();
            _WatchDog = null;
            _jsonContextValue = $"{_jsonContextKey} = {{}}";
        }

        public Task RunAsynk(string scriptName, bool repit = false)
        {
            _currentScript = AppSetting.Scripts.FirstOrDefault(x => x.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase)) ??
                throw new Exception($"Не найден скрипт по имени '{scriptName}'");

            if (repit)
            {
                if (_countAttempts >= _currentScript.Attempts)
                    return null;
                else
                    _countAttempts++;
            }
            else
            {
                _countAttempts = 0;
            }

            if (_currentScript.WatchDog > 0)
            {
                if (WatchDogEnable)
                {
                    WatchDog current = _WatchDog;
                    _WatchDog = new WatchDog(() =>
                    {
                        RunAsynk(scriptName, true);
                    }
                    , _currentScript.WatchDog);

                    current?.Dispose();
                }
            }

            return _browser.LoadUrlAsync(_currentScript.StartUrl)
                 .ContinueWith(x => AddNewContext())
                 .ContinueWith(x => AddScript());
        }

        private void AddScriptContext()
        {
            if (!string.IsNullOrEmpty(_jsonContextValue))
                _browser.EvaluateScriptAsync(_jsonContextValue);
        }

        private void AddNewContext()
        {
            _browser.EvaluateScriptAsync($"{_jsonContextKey} = {{}}");
        }

        private Task AddScript()
        {
            try
            {
                string valueStr = File.ReadAllText(_currentScript.FileScript);
                valueStr = ReplasePasword(valueStr);

                if (!string.IsNullOrEmpty(AppSetting.CommonScript))
                {
                    string valueCommonStr = File.ReadAllText(AppSetting.CommonScript);
                    _browser.EvaluateScriptAsync(valueCommonStr)
                        .ContinueWith((x) => _browser.EvaluateScriptAsync(valueStr));
                }
                else
                {
                    return _browser.EvaluateScriptAsync(valueStr);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return Task.Run(() => { });
        }

        private string ReplasePasword(string source)
        {
            string passwordKey = "{Password:";
            string result = source;
            int startIndex = result.IndexOf(passwordKey);

            while (startIndex != -1)
            {
                int endIndex = result.IndexOf('}', startIndex + 1);

                string login = result.Substring(startIndex + passwordKey.Length, endIndex - startIndex - passwordKey.Length);
                string password = AppSetting.PasswordManager.GetPassword(login) ?? string.Empty;

                result = result.Remove(startIndex, endIndex - startIndex + 1);
                result = result.Insert(startIndex, password);

                startIndex = result.IndexOf(passwordKey, endIndex);
            }

            return result;
        }
    }
}
