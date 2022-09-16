namespace Marketplace.Import
{
    public class ScriptSetting
    {
        public string FileScript { get; set; }
        public string Name { get; set; }
        public string StartUrl { get; set; }
        public string[] CheckHosts { get; set; }
        public string ReportFile { get; set; }
        public int WatchDog { get; set; }
        public int Attempts { get; set; }
    }
}
