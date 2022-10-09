using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Marketplace.Import.Helpers
{
    public class FileWriter
    {
        private readonly Thread _thread;
        private readonly string _fileName;
        private readonly BlockingCollection<string> _values = new BlockingCollection<string>();

        public FileWriter()
        {
            _fileName = GetFileLogName();
            _thread = new Thread(WorkerThread)
            {
                IsBackground = true,
                Name = "FileWriter",
            };
            string folder = Path.GetDirectoryName(_fileName);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            _thread.Start();
        }

        public void WriteLogAsynk(string item)
        {
            _values.Add($"Date:{DateTime.Now} {item}");
        }


        private void WorkerThread()
        {
            while (true)
            {
                string line = _values.Take();
                File.AppendAllLines(_fileName, new string[] { line });
            }
        }
        private static string GetFileLogName()
        {
            string filder = AppSetting.LogsFolder;
            string logFileName = Path.Combine(filder,
                String.IsNullOrEmpty(AppSetting.RunScriptName) ?
                $"{DateTime.Now:yyyy.dd.MM HH.mm.ss.fff}.log" :
                $"{AppSetting.RunScriptName}_{AppSetting.CurrentCredentialID}_{DateTime.Now:yyyy.dd.MM HH.mm.ss.fff}.log");
            return logFileName;
        }

    }
}
