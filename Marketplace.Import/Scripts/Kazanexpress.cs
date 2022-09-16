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
    internal class Kazanexpress
    {
        ChromiumWebBrowser _browser;
        public Kazanexpress(ChromiumWebBrowser browser)
        {
            _browser = browser;
            browser.LocationChanged += (o, e) =>
            {
                CheckHostMask(_browser.Address, //"login.aliexpress.ru",
                    "kazanexpress.ru");
            };
        }

        public Task RunAsynk() =>
            Task.Factory.StartNew(Run);

        private static bool CheckHostMask(string url, params string[] masks)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(url);
            }
            catch
            {
                return false;
            }

            return masks.Any(x => uri.Host.Contains(x));
        }

        public void Run()
        {
            _browser.LoadUrlAsync("https://login.aliexpress.com/")
                .ContinueWith(x => AddScript());
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
