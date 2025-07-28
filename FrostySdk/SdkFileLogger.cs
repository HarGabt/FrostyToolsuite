using System;
using System.IO;

namespace FrostySdk
{
    public static class SdkFileLogger
    {
        private static object locks = new object();
        private const string logName = "sdk.log";
        private static bool IsLogInit = false;

        private static void Init()
        {
            if (IsLogInit)
            {
                return;
            }

            lock (locks)
            {
                File.WriteAllText(logName, $"[{DateTime.Now}] Logger started\n");

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
                using (var stream = File.AppendText(logName))
                {
                    stream.WriteLine($"[{DateTime.Now}] {message}");
                }
            }
        }
    }
}
