using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Marketplace.Import.Helpers
{
    public abstract class QueueActionHelper<T>
    {
        private readonly BlockingCollection<T> _values = new BlockingCollection<T>();
        public void Enqueue(T item)
        {

        }
    }

    public class FileWriter
    {
        private readonly Thread _thread;
        private readonly string _fileName;
        private readonly BlockingCollection<string> _values = new BlockingCollection<string>();

        public FileWriter(string fileName)
        {
            _fileName = fileName;
            _thread = new Thread(WorkerThread)
            {
                IsBackground = true,
                Name = "FileWriter",
            };
            string folder = Path.GetDirectoryName(fileName);

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
    }
}
