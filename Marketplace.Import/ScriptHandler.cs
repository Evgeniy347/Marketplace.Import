using CefSharp;
using CefSharp.WinForms;
using Marketplace.Import.Controls;
using Marketplace.Import.Exceptions;
using Marketplace.Import.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        private ToolStripLabel _statusLabel;
        private bool _stop;

        public ScriptHandler(ChromiumWebBrowser browser, ToolStripLabel statusLabel)
        {
            _browser = browser;
            //Событие изменения статуса рендера страницы
            browser.LoadingStateChanged += OnLoadingStateChanged;

            //Событие console.log
            browser.ConsoleMessage += OnConsoleMessage;
            _statusLabel = statusLabel;
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
                    _browser.InvokeOnUiThreadIfRequired(() => _statusLabel.Text = $"DownLoad ({_currentScript.Name})");
                    _WatchDog?.Dispose();
                    _browser.StartDownload(url);
                }
                else if (e.Message.StartsWith("WatchDogReset"))
                {
                    _WatchDog?.Reset();
                }
                else if (e.Message.StartsWith("StopAppScript"))
                {
                    Stop();
                }
                else if (e.Message.StartsWith("EnableBrowser"))
                {
                    BrowserForm.Instance.EnableBrowser();
                }
                else if (e.Message.StartsWith("DisableBrowser"))
                {
                    BrowserForm.Instance.DisableBrowser();
                }
            }
        }

        private void OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (_stop)
                return;

            //Дожидаемся загрузки страницы
            bool isLoading = !e.CanReload;

            BrowserForm.Instance.FileWriter.WriteLogAsynk($"Address:{_browser.Address} IsLoading:{isLoading}");

            if (_currentScript == null)
                return;

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
                else
                {
                    BrowserForm.Instance.FileWriter.WriteLogAsynk($"Not Check Host");
                }
            }
        }

        public void Stop()
        {
            //_currentScript = null;
            _stop = true;
            _WatchDog?.Dispose();
            _WatchDog = null;
            _jsonContextValue = $"{_jsonContextKey} = {{}}";
            _browser.Invoke((Action)(() => _statusLabel.Text = $"Stop"));

            BrowserForm.CloseForm();
        }

        private void ShowDevToolAsynk()
        {
            _browser.InvokeOnUiThreadIfRequired(() =>
            {
                if (_browser.BrowserCore != null)
                    _browser.ShowDevTools();
                else
                    Task.Run(() => { Thread.Sleep(100); ShowDevToolAsynk(); });
            });
        }

        public Task RunAsynk(string scriptName, bool repit = false)
        {
            _stop = false;
            _currentScript = AppSetting.Scripts.FirstOrDefault(x => x.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase)) ??
                throw new Exception($"Не найден скрипт по имени '{scriptName}'");


            BrowserForm.Instance.FileWriter.WriteLogAsynk($"RunAsynk:{scriptName}");
            _browser.InvokeOnUiThreadIfRequired(() => _statusLabel.Text = $"Run ({scriptName})");

            if (AppSetting.ShowDevelop)
                ShowDevToolAsynk();



            if (AppSetting.ShowDevelop)

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
                        if (BrowserForm.CheckIfDevToolsIsOpenAsync().Result)
                        {
                            BrowserForm.Instance.FileWriter.WriteLogAsynk($"DevToolsOpen. Disable WatchDogReset");
                        }
                        else
                        {
                            BrowserForm.Instance.FileWriter.WriteLogAsynk($"WatchDogReset");
                            RunAsynk(scriptName, true);
                        }
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
            string script = $"{_jsonContextKey} = {{}}";
            if (!string.IsNullOrEmpty(_jsonContextValue))
                script = _jsonContextValue;

            BrowserForm.Instance.FileWriter.WriteLogAsynk($"AddScriptContext:{script}");
            _browser.EvaluateScriptAsync(script);
        }

        private void AddNewContext()
        {
            BrowserForm.Instance.FileWriter.WriteLogAsynk($"AddNewContext");
            _browser.EvaluateScriptAsync($"{_jsonContextKey} = {{}}");
        }

        private Task AddScript()
        {
            try
            {
                if (_currentScript == null)
                    return Task.Run(() => { });

                BrowserForm.Instance.FileWriter.WriteLogAsynk($"AddScript");
                string valueStr = File.ReadAllText(_currentScript.FileScript);
                valueStr = ReplasePasword(valueStr);
                valueStr = GetJSArgumentValue() + Environment.NewLine + valueStr;

                if (!string.IsNullOrEmpty(AppSetting.CommonScript))
                {
                    string valueCommonStr = File.ReadAllText(AppSetting.CommonScript);
                    _browser.EvaluateScriptAsync(valueCommonStr + Environment.NewLine + valueStr);
                }
                else
                {
                    return _browser.EvaluateScriptAsync(valueStr);
                }
            }
            catch (Exception ex)
            {
                BrowserForm.Instance.FileWriter.WriteLogAsynk(ex.ToString());
                if (AppSetting.RunScript)
                    BrowserForm.CloseForm();

                MessageBox.Show(ex.ToString());
            }

            return Task.Run(() => { });
        }

        private string ReplasePasword(string source)
        {
            string result = source;
            if (source.Contains("{Login}") || source.Contains("{Password}"))
            {
                string credentialID = GetCredential();
                if (string.IsNullOrEmpty(credentialID))
                    return result;

                try
                {
                    CredentialEntry credential = AppSetting.PasswordManager.GetCredential(credentialID);

                    result = result
                        .Replace("{Login}", credential.Login)
                        .Replace("{Password}", credential.GetPassword());
                }
                catch
                {
                    Stop();
                    throw;
                }
            }
            return result;
        }

        public string GetCredential()
        {
            string credentialID = AppSetting.CurrentCredentialID;
            if (string.IsNullOrEmpty(credentialID))
                credentialID = _currentScript.DefaultCredential;

            return credentialID;
        }


        public string GetJSArgumentValue()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("MPS_Params = {");

            if (_currentScript != null)
            {
                foreach (INIValue valueIni in _currentScript.Section.Values)
                    stringBuilder.AppendLine($"'{valueIni.Key}': '{valueIni.Value}',");
            }

            if (AppSetting.ArgsParams != null)
            {
                foreach (INIValue valueIni in AppSetting.ArgsParams.DefaulteSection.Values)
                    stringBuilder.AppendLine($"'{valueIni.Key}': '{valueIni.Value}',");
            }

            stringBuilder.AppendLine("};");

            return stringBuilder.ToString();
        }

        public string ReplaceArgumentValue(string value)
        {
            string result = value;
            if (_currentScript != null)
            {
                foreach (INIValue valueIni in _currentScript.Section.Values)
                    result = result.Replace($"{{{valueIni.Key}}}", valueIni.Value);
            }

            result = AppSetting.ReplaceArgumentValue(result);

            return result;
        }
    }
}
