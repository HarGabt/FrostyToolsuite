using System;
using System.IO;

namespace Frosty.Core
{
    public static class FileLogger
    {
        private static readonly object locks = new object();
        private static readonly string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "executor.log");
        private static bool IsLogInit = false;

        public static void Init()
        {
            if (IsLogInit)
            {
                return;
            }

            lock (locks)
            {
                try
                {
                    File.WriteAllText(logPath, $"[{DateTime.Now}] Logger started\n");
                }
                catch
                {
                    // logging must never be able to crash the app
                }

                IsLogInit = true;
            }
        }

        public static void Info(string message)
        {
            if (!IsLogInit)
            {
                Init();
            }

            lock (locks)
            {
                try
                {
                    using (var stream = File.AppendText(logPath))
                    {
                        stream.WriteLine($"[{DateTime.Now}] {message}");
                    }
                }
                catch
                {
                    // logging must never be able to crash the app
                }
            }
        }
    }
}
