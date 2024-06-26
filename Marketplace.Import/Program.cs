﻿// Copyright © 2010-2015 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp;
using CefSharp.WinForms;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Marketplace.Import
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            { 
#if ANYCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif
                if (args != null)
                {
                    string[] namesParams = args.Where(x => x.Contains("=")).Select(x => x.Trim()).ToArray();
                    string[] notNamesParams = args.Where(x => !x.Contains("=")).Select(x => x.Trim()).ToArray();

                    AppSetting.InitArgs(namesParams);
                }

                try
                {
                    if (File.Exists("debug.log"))
                        File.Delete("debug.log");
                }
                catch { }

                // Programmatically enable DPI Aweness
                // Can also be done via app.manifest or app.config
                // https://github.com/cefsharp/CefSharp/wiki/General-Usage#high-dpi-displayssupport
                // If set via app.manifest this call will have no effect.
                //Cef.EnableHighDPISupport();

                if (!Directory.Exists(AppSetting.FolderCache))
                    throw new DirectoryNotFoundException(AppSetting.FolderCache);

                var settings = new CefSettings()
                {
                    //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                    CachePath = AppSetting.FolderCache,
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
                    PersistSessionCookies = true,
                };

                //Example of setting a command line argument
                //Enables WebRTC
                // - CEF Doesn't currently support permissions on a per browser basis see https://bitbucket.org/chromiumembedded/cef/issues/2582/allow-run-time-handling-of-media-access
                // - CEF Doesn't currently support displaying a UI for media access permissions
                //
                //NOTE: WebRTC Device Id's aren't persisted as they are in Chrome see https://bitbucket.org/chromiumembedded/cef/issues/2064/persist-webrtc-deviceids-across-restart
                settings.CefCommandLineArgs.Add("enable-media-stream");
                //https://peter.sh/experiments/chromium-command-line-switches/#use-fake-ui-for-media-stream
                settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
                //For screen sharing add (see https://bitbucket.org/chromiumembedded/cef/issues/2582/allow-run-time-handling-of-media-access#comment-58677180)
                settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");
                
                //Perform dependency check to make sure all relevant resources are in our output directory.
                Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

                var browser = new BrowserForm();
                Application.Run(browser);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return 0;

        }
    }
}
