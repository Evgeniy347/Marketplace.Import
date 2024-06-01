using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Marketplace.Import.Helpers
{
    public class FileWriter
    {
        private volatile object _lock = new object();
        private readonly Thread _thread;
        private readonly string _fileName;
        private readonly StreamWriter _stream;
        private bool _stop;
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

            _stream = new StreamWriter(_fileName);

            _thread.Start();
        }

        public void WriteLogAsynk(string item)
        {
            _values.Add($"Date:{DateTime.Now} {item}");
        }

        public void Stop()
        {
            lock (_lock)
            {
                _stop = true;
                WriteLogAsynk("#StopLog#");
                _values.Add($"");
            }
            _thread.Join();
        }

        private void WorkerThread()
        {
            Task.Run(RemoveLogs);

            while (true)
            {
                try
                {
                    string line = _values.Take();
                    lock (_lock)
                    {
                        //File.AppendAllLines(_fileName, new string[] { line });
                        _stream.WriteLine(line);
                        if (_stop && !string.IsNullOrEmpty(line) && line.Contains("#StopLog#"))
                            return;
                    }
                }
                catch (Exception ex)
                {
                    WriteLogAsynk(ex.ToString());
                }
            }
        }

        private void RemoveLogs()
        {
            string[] files = Directory.GetFiles(AppSetting.LogsFolder, "*.log");
            if (AppSetting.DeleteLogsDays > 0)
            {
                DateTime dateDelete = DateTime.Now.AddDays(-AppSetting.DeleteLogsDays);
                string[] filesDelete = files.Where(x => File.GetCreationTime(x) < dateDelete).ToArray();

                foreach (string file in filesDelete)
                {
                    try
                    {
                        _values.Add($"Удаляем файл лога '{file}'");
                        File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        _values.Add($"Ошибка при удалении файла '{file}' '{e}'");
                    }
                }
            }
        }

        private static string GetFileLogName()
        {
            string logFileName = Path.Combine(AppSetting.LogsFolder,
                string.IsNullOrEmpty(AppSetting.RunScriptName) ?
                $"{DateTime.Now:yyyy.dd.MM HH.mm.ss.fff}.log" :
                $"{AppSetting.RunScriptName}_{AppSetting.CurrentCredentialID}_{DateTime.Now:yyyy.dd.MM HH.mm.ss.fff}.log");
            return logFileName;
        }
    }
}
