using FrostySdk.Ebx;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using static SoundEditorPlugin.Resources.NewWaveResource;

namespace SoundEditorPlugin.Helpers
{
    public class ToolHelper : HelperBase<ToolHelper>
    {
        public ToolHelper()
        {
            BasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            ResourceName = "SoundEditorPlugin.Resources.Tool.lib.zip";
            ToolName = "Tool";
        }

        public async Task<(byte[] spsData, byte[] seekTableData)> ImportSound(string importFileName, string codec, bool isSeekable, int channelCount, bool remix)
        {
            if (State == InitializedState.NotInitialized)
            {
                await InitializeAsync();
            }

            string tempOutput = BasePath + Guid.NewGuid();

            byte[] spsData;
            byte[] seekTableData;

            try
            {
                Process process = new Process();
                ProcessStartInfo processStartInfo = new ProcessStartInfo(ResourcePath + ".");

                processStartInfo.Arguments = $"-sndplayer -fileformatversion1 -{codec} " +
                    $"{(isSeekable ? "-seekable" : "")} {(remix == true ? $"-remix{(channelCount == 1 ? "mono" : "stereo")}" : "")} " +
                    $"\"{importFileName}\" -=\"{tempOutput}\"";

                processStartInfo.UseShellExecute = false;
                processStartInfo.CreateNoWindow = true;
                process.StartInfo = processStartInfo;
                process.EnableRaisingEvents = true;

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0 && !File.Exists(tempOutput + ".sps"))
                {
                    throw new FileFormatException($"Failed to import the file. Error: {process.ExitCode}");
                }
            }
            finally
            {
                string tempSpsFileName = tempOutput + ".sps";
                string tempSekFileName = tempOutput + ".sek";
                string tempSphFileName = tempOutput + ".sph";

                spsData = File.ReadAllBytes(tempSpsFileName);
                seekTableData = isSeekable ? File.ReadAllBytes(tempSekFileName) : null;

                try
                {
                    if (File.Exists(tempSpsFileName))
                    {
                        File.Delete(tempSpsFileName);
                    }

                    if (File.Exists(tempSekFileName))
                    {
                        File.Delete(tempSekFileName);
                    }

                    if (File.Exists(tempSphFileName))
                    {
                        File.Delete(tempSphFileName);
                    }
                }
                catch (Exception ex) { }
            }

            return (spsData, seekTableData);
        }

        public float GetDurationInSecondsFromBuffer(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 12)
            {
                throw new ArgumentException("buffer cannot be null or must be at least 2 bytes long.");
            }

            int header1 = BitConverter.ToInt32(reverseAtIndex(buffer, 4), 0);
            int header2 = BitConverter.ToInt32(reverseAtIndex(buffer, 8), 0);

            int SampleRate = header1 & 0x3FFFF;
            int SamplesCount = header2 & 0x1FFFFFFF;

            return SampleRate != 0 ? SamplesCount / (float)SampleRate : 0f;
        }

        private byte[] reverseAtIndex(byte[] buffer, int index)
        {
            var segment = new ArraySegment<byte>(buffer, index, 4).ToArray();
            Array.Reverse(segment);
            return segment.ToArray();
        }
    }
}
