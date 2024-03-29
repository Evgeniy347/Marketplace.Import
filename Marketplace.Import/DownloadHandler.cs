﻿// Copyright © 2010-2015 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp;
using Marketplace.Import.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private readonly ScriptHandler _scriptHandler;
        private readonly FileWriter _fileWriter;
        private readonly ConcurrentDictionary<string, DownloadItem> _downloadCallbacks = new ConcurrentDictionary<string, DownloadItem>();

        public static DownloadHandler Instance { get; private set; }

        public static void WaitDownloads()
        {
            while (true)
            {
                int count = Instance._downloadCallbacks.Values
                    .SkipWhile(x => x.IsComplete || x.IsCancelled)
                    .Count();

                Instance._fileWriter.WriteLogAsynk($"WaitDownloads:{count}");
                if (count == 0)
                    break;

                Thread.Sleep(1000);
            }

            Instance._fileWriter.WriteLogAsynk($"WaitDownloads:OK");
        }

        public DownloadHandler(ScriptHandler scriptHandler, FileWriter fileWriter)
        {
            Instance = this;
            _scriptHandler = scriptHandler;
            _fileWriter = fileWriter;
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
                    try
                    {
                        string fullPath = GetPath(downloadItem);

                        string pathFiles = Path.GetDirectoryName(fullPath);

                        if (!Directory.Exists(pathFiles))
                            Directory.CreateDirectory(pathFiles);

                        downloadItem.FullPath = fullPath;

                        if (File.Exists(downloadItem.FullPath))
                            File.Delete(downloadItem.FullPath);

                        _fileWriter.WriteLogAsynk($"OnBeforeDownload FullPath:{fullPath} OriginalUrl:{downloadItem.OriginalUrl}");
                        callback.Continue(fullPath, false);
                        _downloadCallbacks[downloadItem.OriginalUrl] = downloadItem;
                    }
                    catch (Exception ex)
                    {
                        _fileWriter.WriteLogAsynk(ex.ToString());
                    }
                }
            }
        }

        private string GetPath(DownloadItem downloadItem)
        {
            string fullPath = _scriptHandler?.CurrentScript?.ReportFile;
            if (string.IsNullOrEmpty(fullPath))
            {
                _fileWriter.WriteLogAsynk("Не задано имя файла или произошла ошибка при определении имени");
                Uri uri = new Uri(downloadItem.OriginalUrl);
                string fileName = downloadItem.SuggestedFileName;
                if (string.IsNullOrEmpty(fileName))
                    fileName = "report.xlsx";

                fullPath = Path.Combine(Path.Combine(AppSetting.FileFolderReport, uri.Host), fileName);
            }

            string credentialID = _scriptHandler.GetCredential();
            fullPath = fullPath
                .Replace("{CredentialID}", credentialID)
                .Replace("{FirstName}", downloadItem.SuggestedFileName);

            fullPath = _scriptHandler.ReplaceArgumentValue(fullPath);
            return fullPath;
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            var handler = OnDownloadUpdatedFired;
            if (handler != null)
            {
                handler(this, downloadItem);
            }

            _downloadCallbacks[downloadItem.OriginalUrl] = downloadItem;
        }

        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            return true;
        }
    }
}
