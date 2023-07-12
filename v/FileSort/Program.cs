using Shell32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileSort
{
    internal class Program
    {
        [STAThread]
        static void Main()
        {

            string[] files = Directory.GetFiles("D:\\Cloud\\Camera Uploads", "*", SearchOption.AllDirectories);

            Regex regex = new Regex(".*(20\\d{6}_\\d{6}).*");
           IEnumerable<MediaFile> filterFiles = files.Select(x => new MediaFile(x)).Where(x => x.ValidDate);

            Console.WriteLine($"files:{files.Length}");
            int count = 0;

            foreach (var file in filterFiles)
            {
                DateTime dateTime = File.GetCreationTime(file.FullName);

                if (dateTime.Year != file.CreateDate.Year ||
                    dateTime.Month != file.CreateDate.Month ||
                    dateTime.Day != file.CreateDate.Day ||
                    dateTime.Hour != file.CreateDate.Hour ||
                    dateTime.Minute != file.CreateDate.Minute)
                {
                    File.SetCreationTime(file.FullName, file.CreateDate);
                    count++;
                }
            }

            Console.WriteLine(count);
            Console.ReadKey();
        }

        public class MediaFile
        {
            static private readonly Regex _regex = new Regex("(\\d{8}_\\d{6})");

            public string FullName { get; }
            public string FileName { get; }
            public MediaFile(string fullName)
            {
                FullName = fullName;
                FileName = Path.GetFileName(fullName);
            }

            public bool ValidDate => CreateDate != default;

            private bool __init_CreateDate = false;
            private DateTime _CreateDate;
            public DateTime CreateDate
            {
                get
                {
                    if (!__init_CreateDate)
                    {
                        MatchCollection matchs = _regex.Matches(FileName);
                        if (matchs.Count == 1)
                        {
                            Match match = matchs[0];
                            try
                            {
                                _CreateDate = DateTime.ParseExact(match.Value, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                            }
                            catch { }
                        }

                        __init_CreateDate = true;
                    }
                    return _CreateDate;
                }
            }

        }
    }
}
