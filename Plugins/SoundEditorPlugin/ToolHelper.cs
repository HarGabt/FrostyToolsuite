using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace SoundEditorPlugin
{
    public static class ToolHelper
    {
        public static bool IsInitialized = false;
        public static string ResoucePath { get; } = Path.Combine(Path.GetTempPath(), "Tool");

        public static async Task InitializeAsync()
        {
            Stream sxStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SoundEditorPlugin.Resources.Tool.lib.zip");
            ZipArchive archive = new ZipArchive(sxStream, ZipArchiveMode.Read, leaveOpen: false);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                Stream entryStream = null;
                FileStream outputStream = null;

                try
                {
                    entryStream = entry.Open();
                    string outputPath = Path.Combine(Path.GetTempPath(), entry.Name);
                    if (File.Exists(outputPath))
                    {
                        entryStream.Dispose();
                        entryStream = entry.Open();
                    }

                    outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    await entryStream.CopyToAsync(outputStream).ConfigureAwait(continueOnCapturedContext: false);
                }
                finally
                {
                    entryStream?.Dispose();
                    outputStream?.Dispose();
                }
            }

            IsInitialized = true;
        }

        public static float GetDurationInSecondsFromBuffer(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 12)
            {
                throw new ArgumentException("buffer cannot be null or must be at least 2 bytes long.");
            }

            int header1 =  BitConverter.ToInt32(reverseAtIndex(buffer, 4), 0);
            int header2 = BitConverter.ToInt32(reverseAtIndex(buffer, 8), 0);

            int SampleRate = header1 & 0x3FFFF;
            int SamplesCount = header2 & 0x1FFFFFFF;

            return SampleRate != 0 ? SamplesCount / (float)SampleRate : 0f;
        }

        private static byte[] reverseAtIndex(byte[] buffer, int index)
        {
            var segment = new ArraySegment<byte>(buffer, index, 4).ToArray();
            Array.Reverse(segment);
            return segment.ToArray();
        }
    }
}
