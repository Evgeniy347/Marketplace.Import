﻿// Copyright © 2010-2015 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp.DevTools.IO;
using Marketplace.Import.Controls;
using Marketplace.Import.Helpers;
using Marketplace.Import.MasterKey;
using CefSharp.WinForms;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CefSharp;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Marketplace.Import
{
    public partial class BrowserForm : Form
    {
#if DEBUG
        private const string Build = "Debug";
#else
        private const string Build = "Release";
#endif
        private readonly string title = "Marketplace.Import (" + Build + ")";
        private readonly ChromiumWebBrowser browser;
        private readonly ScriptHandler _scriptHandler;
        private readonly FileWriter _fileWriter;

        public FileWriter FileWriter => _fileWriter;

        public static BrowserForm Instance { get; private set; }

        public BrowserForm()
        {
            Instance = this;
            InitializeComponent();

            Text = title;
            WindowState = FormWindowState.Maximized;

            browser = new ChromiumWebBrowser("www.google.com");

            _fileWriter = new FileWriter();

            browser.Enabled = string.IsNullOrEmpty(AppSetting.RunScriptName);
            disabledToolStripMenuItem.Checked = !browser.Enabled;

            _scriptHandler = new ScriptHandler(browser, this.toolStripLabel1);
            toolStripContainer.ContentPanel.Controls.Add(browser);

            browser.IsBrowserInitializedChanged += OnIsBrowserInitializedChanged;
            browser.LoadingStateChanged += OnLoadingStateChanged;
            browser.ConsoleMessage += OnBrowserConsoleMessage;
            browser.StatusMessage += OnBrowserStatusMessage;
            browser.TitleChanged += OnBrowserTitleChanged;
            browser.AddressChanged += OnBrowserAddressChanged;
            browser.LoadError += OnBrowserLoadError;

            DownloadHandler downer = new DownloadHandler(_scriptHandler, _fileWriter);
            browser.DownloadHandler = downer;

            var version = string.Format("Chromium: {0}, CEF: {1}, CefSharp: {2}",
               Cef.ChromiumVersion, Cef.CefVersion, Cef.CefSharpVersion);

#if NETCOREAPP
            // .NET Core
            var environment = string.Format("Environment: {0}, Runtime: {1}",
                System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
#else
            // .NET Framework
            var bitness = Environment.Is64BitProcess ? "x64" : "x86";
            var environment = String.Format("Environment: {0}", bitness);
#endif

            DisplayOutput(string.Format("{0}, {1}", version, environment));

            if (!string.IsNullOrEmpty(AppSetting.RunScriptName))
            {
                _scriptHandler.WatchDogEnable = true;
                _scriptHandler.RunAsynk(AppSetting.RunScriptName);
            }

            InitTabScript();
        }

        private void OnBrowserLoadError(object sender, LoadErrorEventArgs e)
        {
            //Actions that trigger a download will raise an aborted error.
            //Aborted is generally safe to ignore
            if (e.ErrorCode == CefErrorCode.Aborted)
                return;

            var errorHtml = string.Format("<html><body><h2>Failed to load URL {0} with error {1} ({2}).</h2></body></html>",
                                              e.FailedUrl, e.ErrorText, e.ErrorCode);

            _ = e.Browser.SetMainFrameDocumentContentAsync(errorHtml);

            //AddressChanged isn't called for failed Urls so we need to manually update the Url TextBox
            this.InvokeOnUiThreadIfRequired(() => urlTextBox.Text = e.FailedUrl);
        }

        private void OnIsBrowserInitializedChanged(object sender, EventArgs e)
        {
            var b = ((ChromiumWebBrowser)sender);
            this.InvokeOnUiThreadIfRequired(() => b.Focus());
        }

        private void OnBrowserConsoleMessage(object sender, ConsoleMessageEventArgs args)
        {
            string line = $"Line: {args.Line}, Source: {args.Source}, Message: {args.Message}";
            DisplayOutput(line);

            if (!string.IsNullOrEmpty(args.Message))
            {
                if (args.Message.StartsWith("MPS_Redirect"))
                {
                    int firslSplit = args.Message.IndexOf('=');
                    string url = args.Message.Remove(0, firslSplit + 1);
                    browser.LoadUrlAsync(url);
                }
            }
        }

        private void OnBrowserStatusMessage(object sender, StatusMessageEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => statusLabel.Text = args.Value);
        }

        private void OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs args)
        {
            SetCanGoBack(args.CanGoBack);
            SetCanGoForward(args.CanGoForward);

            this.InvokeOnUiThreadIfRequired(() => SetIsLoading(!args.CanReload));
        }

        private void OnBrowserTitleChanged(object sender, TitleChangedEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => Text = title + " - " + args.Title);
        }

        private void OnBrowserAddressChanged(object sender, AddressChangedEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => urlTextBox.Text = args.Address);
        }

        private void SetCanGoBack(bool canGoBack)
        {
            this.InvokeOnUiThreadIfRequired(() => backButton.Enabled = canGoBack);
        }

        private void SetCanGoForward(bool canGoForward)
        {
            this.InvokeOnUiThreadIfRequired(() => forwardButton.Enabled = canGoForward);
        }

        private void SetIsLoading(bool isLoading)
        {
            goButton.Text = isLoading ?
                "Stop" :
                "Go";
            goButton.Image = isLoading ?
                Properties.Resources.nav_plain_red :
                Properties.Resources.nav_plain_green;

            HandleToolStripLayout();
        }

        public void DisplayOutput(string output)
        {
            _fileWriter.WriteLogAsynk(output);
            this.InvokeOnUiThreadIfRequired(() =>
            {
                try
                {
                    outputLabel.Text = output;
                }
                catch
                { }
            }
            );
        }

        private void HandleToolStripLayout(object sender, LayoutEventArgs e)
        {
            HandleToolStripLayout();
        }

        private void HandleToolStripLayout()
        {
            var width = toolStrip1.Width;
            foreach (ToolStripItem item in toolStrip1.Items)
            {
                if (item != urlTextBox)
                {
                    width -= item.Width - item.Margin.Horizontal;
                }
            }
            urlTextBox.Width = Math.Max(0, width - urlTextBox.Margin.Horizontal - 18);
        }

        private void onFormClosing(object sender, FormClosingEventArgs e)
        {
            ExitMenuItemClick(sender, e);
        }

        private void ExitMenuItemClick(object sender, EventArgs e)
        { 
            Instance._fileWriter.WriteLogAsynk($"ExitMenuItemClick"); 
            RunExitThread();
        }

        private void GoButtonClick(object sender, EventArgs e)
        {
            LoadUrl(urlTextBox.Text);
        }

        private void BackButtonClick(object sender, EventArgs e)
        {
            browser.Back();
        }

        private void ForwardButtonClick(object sender, EventArgs e)
        {
            browser.Forward();
        }

        private void UrlTextBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            LoadUrl(urlTextBox.Text);
        }

        private void LoadUrl(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                browser.Load(url);
            }
        }

        private void ShowDevToolsMenuItemClick(object sender, EventArgs e)
        {
            browser.ShowDevTools();
        }

        private void scriptsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void InitTabScript()
        {
            ToolStripItem[] controls = AppSetting.Scripts
                .Select(CreateControlFromScript)
                .ToArray();

            scriptsToolStripMenuItem.DropDownItems.AddRange(controls);
        }

        private ToolStripItem CreateControlFromScript(ScriptSetting script)
        {
            ToolStripMenuItem item = new ToolStripMenuItem()
            {
                Name = script.Name,
                Size = new System.Drawing.Size(180, 22),
                Text = $"Run '{script.Name}'",
            };

            item.Click += (o, e) => { _scriptHandler.RunAsynk(item.Name); };

            return item;
        }

        private void urlTextBox_Click(object sender, EventArgs e)
        {

        }

        private void toolStripContainer_ContentPanel_Load(object sender, EventArgs e)
        {

        }

        private void masterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MasterKeyForm keyForm = new MasterKeyForm();
            keyForm.Show();
        }

        private void disabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            browser.Enabled = !browser.Enabled;
            disabledToolStripMenuItem.Checked = !browser.Enabled;
        }

        public void EnableBrowser()
        {
            this.InvokeOnUiThreadIfRequired(() =>
            {

                disabledToolStripMenuItem.Checked = false;
                browser.Enabled = !disabledToolStripMenuItem.Checked;
            });
        }

        public void DisableBrowser()
        {
            this.InvokeOnUiThreadIfRequired(() =>
            {
                disabledToolStripMenuItem.Checked = true;
                browser.Enabled = !disabledToolStripMenuItem.Checked;
            });
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _scriptHandler.Stop();
        }

        public static Task<bool> CheckIfDevToolsIsOpenAsync()
        {
            return Cef.UIThreadTaskFactory.StartNew(() =>
            {
                return Instance.browser.GetBrowserHost().HasDevTools;
            });
        }


        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBoxDisable_Click(object sender, EventArgs e)
        {

        }

        internal static void CloseForm()
        {
            Instance._fileWriter.WriteLogAsynk($"CloseForm RunScript:{AppSetting.RunScript} ShowDevelop:{AppSetting.ShowDevelop}");

            if (AppSetting.RunScript && !AppSetting.ShowDevelop) 
                RunExitThread();
        }

        public static void RunExitThread()
        {
            Thread thread = new Thread(() =>
            {
                DownloadHandler.WaitDownloads();

                Instance._fileWriter.WriteLogAsynk($"Start CloseDevTools");
                Cef.UIThreadTaskFactory.StartNew(() =>
                {
                    var host = Instance.browser.GetBrowserHost();
                    Instance._fileWriter.WriteLogAsynk($"HasDevTools:{host.HasDevTools}");
                    if (host.HasDevTools)
                        host.CloseDevTools();
                }).Wait();

                Instance._fileWriter.WriteLogAsynk($"End CloseDevTools");

                Thread.Sleep(500);
                Instance._fileWriter.Stop();
                Application.Exit();
            })
            {
                IsBackground = true,
                Name = "ExitThread",
            };

            thread.Start();
        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {

        }
    }
}
