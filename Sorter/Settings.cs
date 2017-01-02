using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentSorter
{
    public class Settings
    {
        private static readonly Settings _settingsInstance = new Settings();

        public string InputFileName { get; private set; }
        public string OutputFileName { get; private set; }
        public bool UseRealWordOptimization { get; private set; }


        public static Settings Instance { get { return _settingsInstance; } }

        private Settings()
        {
            InputFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["InputFile"]);
            OutputFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["OutputFile"]);
            var isWordOptimization = false;
            bool.TryParse(ConfigurationManager.AppSettings["UseRealWordOptimization"], out isWordOptimization);
            UseRealWordOptimization = isWordOptimization;
        }

        
    }
}
