using FrostyCore;
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
using NAudio.Wave;
using static SoundEditorPlugin.Resources.NewWaveResource;
using System.Threading;

namespace SoundEditorPlugin.Helpers
{
    public class VgmStreamHelper : HelperBase<VgmStreamHelper>
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public VgmStreamHelper()
        {
            BasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            ResourceName = "SoundEditorPlugin.Resources.vgmstream.zip";
            ToolName = "vgmstream-cli.exe";
        }

        public async Task<short[]> Decode(byte[] soundBuffer)
        {
            if (State == InitializedState.Initializing)
            {
                await _semaphore.WaitAsync();
                _semaphore.Release();
            }
            else if (State == InitializedState.NotInitialized)
            {
                await InitializeAsync();
            }

            string tempInputFileName = BasePath + Guid.NewGuid() + ".sps";
            string tempOutputFileName = BasePath + Guid.NewGuid() + ".wav";
            File.WriteAllBytes(tempInputFileName, soundBuffer);

            try
            {
                Process process = new Process();
                ProcessStartInfo processStartInfo = new ProcessStartInfo(ResourcePath + ".");

                processStartInfo.Arguments = $"-o {tempOutputFileName} {tempInputFileName}";

                processStartInfo.UseShellExecute = false;
                processStartInfo.CreateNoWindow = true;
                process.StartInfo = processStartInfo;
                process.EnableRaisingEvents = true;

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0 && !File.Exists(tempOutputFileName))
                {
                    throw new FileFormatException($"Failed to decode the file. Error: {process.ExitCode}");
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(tempInputFileName))
                    {
                        File.Delete(tempInputFileName);
                    }
                }
                catch (Exception ex) { }
            }

            short[] returnValue;

            // Read the WAV file and return the short[] full of samples
            using (NativeReader reader = new NativeReader(new FileStream(tempOutputFileName, FileMode.Open, FileAccess.Read)))
            {
                reader.Position = 22;
                ushort channels = reader.ReadUShort();
                uint sampleRate = reader.ReadUInt();

                reader.Position = 34;
                ushort bitsPerSample = reader.ReadUShort();

                reader.Position = 40;
                uint dataLength = reader.ReadUInt();

                int numSamples = (int)dataLength / (bitsPerSample / 8);
                short[] samples = new short[numSamples];

                for (int i = 0; i < numSamples; i++)
                {
                    samples[i] = reader.ReadShort();
                }

                returnValue = samples;
            }

            File.Delete(tempOutputFileName);
            return returnValue;
        }
    }
}
