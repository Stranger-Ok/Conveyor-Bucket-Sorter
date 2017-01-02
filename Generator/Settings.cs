using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentGenerator
{
    public class Settings
    {
        private static readonly Settings _settingsInstance = new Settings();

        public int MaxNumberInLine { get; private set; }
        public int MaxFileSizeInMegabytes { get; private set; }
        public int MaxWordsCountInLine { get; private set; }
        public List<string> WordsList { get; private set; }
        public string OutputFilePath { get; private set; }

        public long AveragelinesCountInOneMbOfData { get; private set; }

        public static Settings Instance { get { return _settingsInstance; } }

        private Settings()
        {
            MaxNumberInLine = int.Parse(ConfigurationManager.AppSettings["MaxNumberValue"]);
            MaxFileSizeInMegabytes = int.Parse(ConfigurationManager.AppSettings["MaxFileSizeInMegabytes"]);
            MaxWordsCountInLine = int.Parse(ConfigurationManager.AppSettings["MaxWordsCountInString"]);
            WordsList = ConfigurationManager.AppSettings["WordsForStringGenerator"].Split(';').ToList();
            OutputFilePath = ConfigurationManager.AppSettings["OutputFilePath"];
            long maxBytesInString = $"{MaxNumberInLine}. ".Length * sizeof(Char) + WordsList.OrderByDescending(s => s.Length).First().Length * sizeof(Char) * MaxWordsCountInLine;
            long minBytesInString = $"0. ".Length * sizeof(Char);
            AveragelinesCountInOneMbOfData = 1024 * 1024 / ((maxBytesInString + minBytesInString) / 2);
        }

        
    }
}
