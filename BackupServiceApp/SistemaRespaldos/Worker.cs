using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SistemaRespaldos.Configuration;

namespace SistemaRespaldos
{
    public class Worker : BackgroundService
    {
        private readonly IConfiguration ftpConfiguration;
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            this.configuration = configuration;
            this.ftpConfiguration = new ConfigurationBuilder()
                .AddJsonFile("backup-configuration.json")
                .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConfigurationModel configurationModel = GetConfiguration();
                StartBackupProcess(configurationModel);
                _logger.LogInformation("BackupDone", DateTimeOffset.Now);
                await Task.Delay(TimeSpan.FromMinutes(configurationModel.BackupIntervalInMinutes), stoppingToken);
            }
        }

        private ConfigurationModel GetConfiguration()
        {
            ConfigurationModel configurationModel = new ConfigurationModel()
            {
                DestinationFtpUrl = ftpConfiguration["DestinationFtpUrl"],
                FoldersToBackup = ftpConfiguration.GetSection("FoldersToBackup").GetChildren().Select(e => e.Value),
                BackupIntervalInMinutes = double.Parse(ftpConfiguration["BackupIntervalInMinutes"]),
                TempCompressionDir = configuration["TempCompressionDir"],
                FtpPassword = ftpConfiguration["FtpPassword"],
                FtpUserName = ftpConfiguration["FtpUserName"]
            };
            return configurationModel;
        }

        private void StartBackupProcess(ConfigurationModel configuration)
        {
            var directoriesMap = CompressAllDirectories(configuration.FoldersToBackup, configuration.TempCompressionDir);
            SendFiles(directoriesMap, configuration);
        }


        private IDictionary<string, string> CompressAllDirectories(IEnumerable<string> foldersToBackup, string compressionDir)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            foreach (var path in foldersToBackup)
            {
                if (result.ContainsKey(path))
                {
                    continue;
                }
                if (Directory.Exists(path))
                {
                    string fileName = new DirectoryInfo(path).Name;
                    string destinationFileName = compressionDir + "/" + fileName + "_" + FormatDate(DateTime.Now) + ".zip";
                    ZipFile.CreateFromDirectory(path, destinationFileName);
                    result[path] = destinationFileName;
                }
                else
                {
                    _logger.LogWarning("Path '" + path + "' not found");
                }
            }
            return result;
        }

        private string FormatDate(DateTime dateTime)
        {
            return $"{dateTime.Year}-{dateTime.Month.ToString().PadLeft(2, '0')}-{dateTime.Day.ToString().PadLeft(2, '0')}_({dateTime.Ticks})";
        }

        private void SendFiles(IDictionary<string, string> paths, ConfigurationModel configuration)
        {
            foreach(var path in configuration.FoldersToBackup)
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(configuration.DestinationFtpUrl + "/" + Path.GetFileName(paths[path]));
                request.Method = WebRequestMethods.Ftp.UploadFile;

                // This example assumes the FTP site uses anonymous logon.
                request.Credentials = new NetworkCredential(configuration.FtpUserName, configuration.FtpPassword);

                // Copy the contents of the file to the request stream.
                byte[] fileContents = File.ReadAllBytes(paths[path]);

                request.ContentLength = fileContents.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Console.WriteLine($"Upload File Complete, status {response.StatusDescription}");
                }
            }
        }
    }
}
