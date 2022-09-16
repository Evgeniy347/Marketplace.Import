using CefSharp.Handler;
using CefSharp.MinimalExample.WinForms.Extensions;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CefSharp.MinimalExample.WinForms.Scripts
{
    internal class Aliexpress
    {
        ChromiumWebBrowser _browser;
        private string _jsonContextValue;
        private string _jsonContextKey = "MPS_Aliexpress_Context";
        public Aliexpress(ChromiumWebBrowser browser)
        {
            _browser = browser;
            //Событие изменения статуса рендера страницы
            browser.LoadingStateChanged += OnLoadingStateChanged;
            //Событие console.log
            browser.ConsoleMessage += OnConsoleMessage;
        }

        private void OnConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
                if (e.Message.StartsWith(_jsonContextKey))
                    _jsonContextValue = e.Message;

                if (e.Message.StartsWith("FileAliexpressUrl:"))
                {
                    string url = e.Message.Replace("FileAliexpressUrl:", "");
                    _browser.StartDownload(url);  
                }
            } 
        }

        private void OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            //Дожидаемся загрузки страницы
            bool isLoading = !e.CanReload;
            if (!isLoading)
            {
                //Проверяем url страницы
                if (_browser.Address.CheckHostMask("seller.aliexpress.ru", "aliexpress.ru", "login.aliexpress.com"))
                {
                    //Добавляем контекст операции
                    AddScriptContext();

                    //Добавляем скрипт 
                    AddScript();
                }
            }
        }

        public Task RunAsynk() =>
            Task.Factory.StartNew(Run);


        public void Run()
        {
            _browser.LoadUrlAsync("https://seller.aliexpress.ru/orders/orders/")
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

        private void AddScript()
        {
            try
            {
                string valueStr = File.ReadAllText(@"C:\Users\GTR\source\repos\TestOpenWebPage\WebBrowserDownloadFile\Scripts\Aliexpress.js");
                _browser.EvaluateScriptAsync(valueStr);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
