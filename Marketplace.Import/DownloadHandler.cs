﻿// Copyright © 2010-2015 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Marketplace.Import
{
    /// <summary>
    /// Обработчик загрузки файлов
    /// </summary>
    internal class DownloadHandler : IDownloadHandler
    {
        public event EventHandler<DownloadItem> OnBeforeDownloadFired;

        public event EventHandler<DownloadItem> OnDownloadUpdatedFired;

        private BrowserForm _mainForm;
        private ScriptHandler _scriptHandler;

        public DownloadHandler(BrowserForm form, ScriptHandler scriptHandler)
        {
            _mainForm = form;
            _scriptHandler = scriptHandler;
        }

        public void OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            var handler = OnBeforeDownloadFired;
            if (handler != null)
                handler(this, downloadItem);

            if (!callback.IsDisposed)
            {
                using (callback)
                {
                    string fullPath = _scriptHandler.CurrentScript.ReportFile;
                    if (string.IsNullOrEmpty(fullPath))
                    {
                        Uri uri = new Uri(downloadItem.OriginalUrl);
                        fullPath = Path.Combine(Path.Combine(AppSetting.FileFolderReport, uri.Host), "report.xlsx");
                    }

                    string pathFiles = Path.GetDirectoryName(fullPath);

                    if (!Directory.Exists(pathFiles))
                        Directory.CreateDirectory(pathFiles);

                    downloadItem.FullPath = fullPath;

                    if (File.Exists(downloadItem.FullPath))
                        File.Delete(downloadItem.FullPath);

                    callback.Continue(fullPath, false);

                    if (AppSetting.RunScript)
                    {

                        Thread thread = new Thread(() =>
                        {
                            while (!callback.IsDisposed)
                                Thread.Sleep(500);

                            Thread.Sleep(5000);
                            Application.Exit();
                        });

                        thread.Start();
                    }
                }
            }
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            var handler = OnDownloadUpdatedFired;
            if (handler != null)
            {
                handler(this, downloadItem);
            }
        }

        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            return true;
        }

    }
}