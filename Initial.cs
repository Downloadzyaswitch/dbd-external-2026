using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FileMonitorApp
{
    public class FileMonitorService
    {
        private readonly List<string> _pathsToMonitor;
        private readonly int _checkIntervalSeconds;
        private readonly string _logFilePath;
        private readonly EmailNotificationService _emailService;
        private Dictionary<string, long> _previousFileSizes = new();

        public FileMonitorService(List<string> pathsToMonitor, int checkIntervalSeconds, string logFilePath, EmailNotificationService emailService)
        {
            _pathsToMonitor = pathsToMonitor;
            _checkIntervalSeconds = checkIntervalSeconds;
            _logFilePath = logFilePath;
            _emailService = emailService;
        }

        public async Task StartMonitoringAsync()
        {
            LogMessage("File monitoring service started.");

            while (true)
            {
                try
                {
                    await Task.Delay(_checkIntervalSeconds * 1000);
            CheckForChanges();
                }
                catch (Exception ex)
                {
                    LogMessage($"Error during monitoring: {ex.Message}");
                }
            }
        }

        private void CheckForChanges()
        {
            foreach (var path in _pathsToMonitor)
            {
                if (!Directory.Exists(path))
                {
                    LogMessage($"Directory not found: {path}");
                    continue;
                }

                var currentFiles = new Dictionary<string, long>();
                try
                {
                    var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                currentFiles[file] = info.Length;

               
                if (!_previousFileSizes.ContainsKey(file))
                {
                    var message = $"NEW FILE: {file} ({info.Length} bytes)";
                    LogMessage(message);
            _emailService.SendNotification("File Monitor Alert", message);
                }
               
                else if (_previousFileSizes[file] != info.Length)
                {
            var message = $"MODIFIED: {file} (was {_previousFileSizes[file]}, now {info.Length} bytes)";
            LogMessage(message);
            _emailService.SendNotification("File Monitor Alert", message);
                }
            }
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogMessage($"Access denied to {path}: {ex.Message}");
                }

                _previousFileSizes = currentFiles;
            }
        }

        private void LogMessage(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(logEntry);

            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
}
