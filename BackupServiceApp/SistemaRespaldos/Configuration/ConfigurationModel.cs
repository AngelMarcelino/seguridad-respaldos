using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaRespaldos.Configuration
{
    public class ConfigurationModel
    {
        public double BackupIntervalInMinutes { get; set; }
        public string DestinationFtpUrl { get; set; }
        public string FtpUserName { get; set; }
        public string FtpPassword { get; set; }
        public IEnumerable<string> FoldersToBackup { get; set; }
        public string TempCompressionDir { get; set; }
    }
}
