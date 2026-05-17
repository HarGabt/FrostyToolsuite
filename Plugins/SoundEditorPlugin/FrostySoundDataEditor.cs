using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Frosty.Core;
using FrostySdk.Managers;
using FrostySdk.Managers.Entries;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using WaveFormatExtensible = SharpDX.Multimedia.WaveFormatExtensible;
using FrostySdk;
using SoundEditorPlugin.Resources;
using System.Diagnostics;
using FrostySdk.Ebx;
using System.Collections;
using SoundEditorPlugin.Helpers;

namespace SoundEditorPlugin
{
    public static class EALayer3
    {
        public delegate void AudioCallback([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] short[] data, int count, StreamInfo info);

        [StructLayout(LayoutKind.Sequential)]
        public struct StreamInfo
        {
            public int streamIndex;
            public int numChannels;
            public int sampleRate;
        }

        [DllImport("../thirdparty/ealayer3.dll", EntryPoint = "Decode")]
        public static extern void Decode([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] buffer, int length, AudioCallback callback);
    }

    public static class Pcm16b
    {
        public static short[] Decode(byte[] soundBuffer)
        {
            using (NativeReader reader = new NativeReader(new MemoryStream(soundBuffer)))
            {
                ushort blockType = reader.ReadUShort();
                ushort blockSize = reader.ReadUShort(Endian.Big);
                byte compressionType = reader.ReadByte();

                int channelCount = (reader.ReadByte() >> 2) + 1;
                ushort sampleRate = reader.ReadUShort(Endian.Big);
                int totalSampleCount = reader.ReadInt(Endian.Big) & 0x00ffffff;

                List<short>[] channels = new List<short>[channelCount];
                for (int i = 0; i < channelCount; i++)
                    channels[i] = new List<short>();

                while (reader.Position <= reader.Length)
                {
                    blockType = reader.ReadUShort();
                    blockSize = reader.ReadUShort(Endian.Big);

                    if (blockType == 0x45)
                        break;

                    uint samples = reader.ReadUInt(Endian.Big);

                    for (int i = 0; i < samples; i++)
                    {
                        for (int j = 0; j < channelCount; j++)
                            channels[j].Add(reader.ReadShort(Endian.Big));
                    }
                }

                short[] outBuffer = new short[channels[0].Count * channelCount];
                for (int i = 0; i < channels[0].Count; i++)
                {
                    for (int j = 0; j < channelCount; j++)
                    {
                        outBuffer[(i * channelCount) + j] = channels[j][i];
                    }
                }

                return outBuffer;
            }
        }
    }
    public static class XAS
    {
        public static short[] Decode(byte[] soundBuffer)
        {
            using (NativeReader reader = new NativeReader(new MemoryStream(soundBuffer)))
            {
                ushort blockType = reader.ReadUShort();
                ushort blockSize = reader.ReadUShort(Endian.Big);
                byte compressionType = reader.ReadByte();

                int channelCount = (reader.ReadByte() >> 2) + 1;
                ushort sampleRate = reader.ReadUShort(Endian.Big);
                int totalSampleCount = reader.ReadInt(Endian.Big) & 0x00ffffff;

                List<short>[] channels = new List<short>[channelCount];
                for (int i = 0; i < channelCount; i++)
                    channels[i] = new List<short>();

                while (reader.Position <= reader.Length)
                {
                    blockType = reader.ReadUShort();
                    blockSize = reader.ReadUShort(Endian.Big);

                    if (blockType == 0x45)
                        break;

                    uint samples = reader.ReadUInt(Endian.Big);

                    byte[] buffer = null;
                    short[] blockBuffer = new short[32];
                    int[] consts1 = new int[4] { 0, 240, 460, 392 };
                    int[] consts2 = new int[4] { 0, 0, -208, -220 };

                    for (int i = 0; i < (blockSize / 76 / channelCount); i++)
                    {
                        for (int j = 0; j < channelCount; j++)
                        {
                            buffer = reader.ReadBytes(76);

                            for (int k = 0; k < 4; k++)
                            {
                                blockBuffer[0] = (short)(buffer[k * 4 + 0] & 0xF0 | buffer[k * 4 + 1] << 8);
                                blockBuffer[1] = (short)(buffer[k * 4 + 2] & 0xF0 | buffer[k * 4 + 3] << 8);

                                int index4 = (int)buffer[k * 4] & 0x0F;
                                int num10 = (int)buffer[k * 4 + 2] & 0x0F;
                                int index5 = 2;

                                while (index5 < 32)
                                {
                                    int num11 = ((int)buffer[12 + k + index5 * 2] & 240) >> 4;
                                    if (num11 > 7)
                                        num11 -= 16;

                                    int num12 = blockBuffer[index5 - 1] * consts1[index4] + blockBuffer[index5 - 2] * consts2[index4];

                                    blockBuffer[index5] = (short)(num12 + (num11 << 20 - num10) + 128 >> 8);
                                    if (blockBuffer[index5] > (int)short.MaxValue)
                                        blockBuffer[index5] = (int)short.MaxValue;
                                    else if (blockBuffer[index5] < (int)short.MinValue)
                                        blockBuffer[index5] = (int)short.MinValue;

                                    int num13 = (int)buffer[12 + k + index5 * 2] & 15;
                                    if (num13 > 7)
                                        num13 -= 16;

                                    int num14 = blockBuffer[index5] * consts1[index4] + blockBuffer[index5 - 1] * consts2[index4];

                                    blockBuffer[index5 + 1] = (short)(num14 + (num13 << 20 - num10) + 128 >> 8);
                                    if (blockBuffer[index5 + 1] > (int)short.MaxValue)
                                        blockBuffer[index5 + 1] = (int)short.MaxValue;
                                    else if (blockBuffer[index5 + 1] < (int)short.MinValue)
                                        blockBuffer[index5 + 1] = (int)short.MinValue;

                                    index5 += 2;
                                }

                                channels[j].AddRange(blockBuffer);
                            }

                            uint sampleSize = (samples < 128) ? samples : 128;
                            samples -= sampleSize;
                        }
                    }
                }

                short[] outBuffer = new short[channels[0].Count * channelCount];
                for (int i = 0; i < channels[0].Count; i++)
                {
                    for (int j = 0; j < channelCount; j++)
                    {
                        outBuffer[(i * channelCount) + j] = channels[j][i];
                    }
                }

                return outBuffer;
            }
        }
    }

    public class SoundDataTrack : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string ExtraName
        {
            get
            {
                if (!IsLoaded)
                {
                    return " - Loading...";
                }
                else
                {
                    return "";
                }
            }
        }
        public int CodecUnformatted { get; set; }
        private string codec;
        public string Codec { get => codec; set { codec = value; NotifyPropertyChanged(); } }
        private double duration;
        public double Duration { get => duration; set { duration = value; NotifyPropertyChanged(); } }
        public int SegmentCount { get; set; }
        public string Language { get; set; }
        private ImageSource waveform;
        public ImageSource WaveForm { get => waveform; set { waveform = value; NotifyPropertyChanged(); } }
        private int samplerate;
        public int SampleRate { get => samplerate; set { samplerate = value; NotifyPropertyChanged(); } }
        private int channelcount;
        public int ChannelCount { get => channelcount; set { channelcount = value; NotifyPropertyChanged(); } }
        public short[] Samples { get; set; }
        public uint LoopStart { get; set; }
        public uint LoopEnd { get; set; }
        public int ChunkIndex { get; set; }
        public Guid ChunkId { get; set; }
        public int SegmentIndex { get; set; }
        public int VariationIndex { get; set; }
        private bool isloaded;
        public bool IsLoaded { get => isloaded; set { isloaded = value; NotifyPropertyChanged(); NotifyPropertyChanged("ExtraName"); } }

        private double progress;
        public double Progress { get => progress; set { progress = value; NotifyPropertyChanged(); } }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SoundWave : IDisposable
    {
        public SoundDataTrack track;
        public event RoutedEventHandler OnFinishedPlaying;
        public double Progress => (voice.State.SamplesPlayed - (loopPtr / track.ChannelCount) < 0) ? voice.State.SamplesPlayed / (double)(SampleCount / track.ChannelCount) : (voice.State.SamplesPlayed - (loopPtr / track.ChannelCount)) / (double)(SampleCount / track.ChannelCount);
        public long SampleCount => track.Samples.Length;

        private SourceVoice voice;
        private AudioBuffer buffer;
        private int bufferPtr;
        private long loopPtr;
        private long loopCount;

        public SoundWave(SoundDataTrack inTrack, AudioPlayer player)
        {
            track = inTrack;

            WaveFormatExtensible format = new WaveFormatExtensible(track.SampleRate, 16, track.ChannelCount);
            switch (track.ChannelCount)
            {
                case 2: format.ChannelMask = Speakers.FrontLeft | Speakers.FrontRight; break;
                case 4: format.ChannelMask = Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight; break;
                case 6: format.ChannelMask = Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight; break;
                default: format.ChannelMask = 0; break;
            }

            voice = new SourceVoice(player.AudioSystem, format, true);
            voice.SetOutputVoices(new VoiceSendDescriptor(player.OutputVoice));

            bool ForceStereo = Config.Get<bool>("ForceStereo", true);
            if (ForceStereo)
            {
                // To force stereo (2-channel) output: take input channel count (track channel count) * output channel count (2)
                // make an array of floats representing volume level (range is 0-100%, where 100 = 1.0f) and turn every channel up to 100%
                List<float> fMatrix = new List<float>();

                for (int i = 0; i < track.ChannelCount; i++)
                {
                    fMatrix.Add(1.0f); // L channel
                    fMatrix.Add(1.0f); // R channel
                }

                voice.SetOutputMatrix(track.ChannelCount, 2, fMatrix.ToArray());
            }

            voice.BufferEnd += Voice_BufferEnd;

            Voice_BufferEnd(IntPtr.Zero);

            voice.Start();
        }

        private const int MAX_BUFFER_SIZE = 4096;
        private void Voice_BufferEnd(IntPtr obj)
        {
            if (bufferPtr < SampleCount)
            {
                int bufferSize = (SampleCount - bufferPtr > MAX_BUFFER_SIZE * track.ChannelCount) ? MAX_BUFFER_SIZE * track.ChannelCount : (int)(SampleCount - bufferPtr);
                DataStream DS = new DataStream(bufferSize * sizeof(short), true, true);
                buffer = new AudioBuffer
                {
                    Stream = DS,
                    AudioBytes = (int)DS.Length,
                    Flags = BufferFlags.None
                };

                // interleave channels
                while (DS.Position < DS.Length)
                {
                    DS.Write(track.Samples[bufferPtr]);
                    bufferPtr++;

                    if (track.LoopEnd != 0 && bufferPtr == track.LoopEnd)
                    {
                        loopPtr += bufferPtr - track.LoopStart;
                        loopCount++;

                        bufferPtr = (int)track.LoopStart;
                    }
                }

                voice.SubmitSourceBuffer(buffer, null);
            }
            else
            {
                OnFinishedPlaying?.Invoke(this, new RoutedEventArgs());
            }
        }

        public void Dispose()
        {
            voice.Stop();
            voice.DestroyVoice();
            voice.Dispose();
        }
    }

    public class AudioPlayer : IDisposable
    {
        public XAudio2 AudioSystem { get; }
        public MasteringVoice OutputVoice { get; }
        public double Progress => currentSound?.Progress ?? 0.0;
        public bool IsPlaying { get; private set; }

        private SoundWave currentSound;

        public AudioPlayer()
        {
            AudioSystem = new XAudio2();
            OutputVoice = new MasteringVoice(AudioSystem, 8);
        }

        public void PlaySound(SoundDataTrack track)
        {
            SoundDispose();

            currentSound = new SoundWave(track, this);
            IsPlaying = true;
            currentSound.OnFinishedPlaying += CurrentSound_OnFinishedPlaying;
        }

        private void CurrentSound_OnFinishedPlaying(object sender, RoutedEventArgs e)
        {
            IsPlaying = false;
            //SoundDispose();
        }

        public void SoundDispose()
        {
            IsPlaying = false;

            if (currentSound == null)
                return;

            var tmpSound = currentSound;
            currentSound = null;
            tmpSound.Dispose();
        }

        public void Dispose()
        {
            SoundDispose();

            OutputVoice.Dispose();
            AudioSystem.Dispose();
        }
    }

    [TemplatePart(Name = PART_TracksListBox, Type = typeof(ListView))]
    [TemplatePart(Name = PART_PlayButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_StopButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_VolumeSlider, Type = typeof(Slider))]
    [TemplatePart(Name = PART_SoundExportMenuItem, Type = typeof(MenuItem))]
    [TemplatePart(Name = PART_SoundImportMenuItem, Type = typeof(MenuItem))]
    public class FrostySoundDataEditor : FrostyAssetEditor
    {
        private const string PART_TracksListBox = "PART_TracksListBox";
        private const string PART_PlayButton = "PART_PlayButton";
        private const string PART_StopButton = "PART_StopButton";
        private const string PART_VolumeSlider = "PART_VolumeSlider";
        private const string PART_SoundExportMenuItem = "PART_SoundExportMenuItem";
        private const string PART_SoundImportMenuItem = "PART_SoundImportMenuItem";

        public static readonly DependencyProperty TracksListProperty = DependencyProperty.Register("TracksList", typeof(ObservableCollection<SoundDataTrack>), typeof(FrostySoundDataEditor), new FrameworkPropertyMetadata(null));
        public ObservableCollection<SoundDataTrack> TracksList
        {
            get => (ObservableCollection<SoundDataTrack>)GetValue(TracksListProperty);
            set => SetValue(TracksListProperty, value);
        }

        public bool IsPlaying => audioPlayer != null && audioPlayer.IsPlaying;

        private ListView tracksListBox;
        private Button playButton;
        private Button stopButton;
        private Slider volumeSlider;
        private AudioPlayer audioPlayer;
        private bool bFirstTime = true;

        public FrostySoundDataEditor(ILogger inLogger)
            : base(inLogger)
        {
        }

        static FrostySoundDataEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FrostySoundDataEditor), new FrameworkPropertyMetadata(typeof(FrostySoundDataEditor)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            tracksListBox = GetTemplateChild(PART_TracksListBox) as ListView;

            playButton = GetTemplateChild(PART_PlayButton) as Button;
            playButton.Click += PlayButton_Click;

            stopButton = GetTemplateChild(PART_StopButton) as Button;
            stopButton.Click += StopButton_Click;

            volumeSlider = GetTemplateChild(PART_VolumeSlider) as Slider;

            volumeSlider.Value = Math.Min(Config.Get<float>("SoundVolume", 20.0f), 100);

            volumeSlider.ValueChanged += VolumeSlider_ValueChanged;

            tracksListBox.SelectionChanged += TracksListBox_SelectionChanged;
            MenuItem mi = GetTemplateChild(PART_SoundExportMenuItem) as MenuItem;
            mi.Click += SoundExportMenuItem_Click;
            mi = GetTemplateChild(PART_SoundImportMenuItem) as MenuItem;
            mi.Click += SoundImportMenuItem_Click;
            Loaded += FrostySoundDataEditor_Loaded;

            TracksList = new ObservableCollection<SoundDataTrack>();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            audioPlayer.SoundDispose();
            stopButton.IsEnabled = false;

            if (tracksListBox.SelectedItem != null)
                playButton.IsEnabled = true;
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(tracksListBox.SelectedItem is SoundDataTrack currentTrack) || !currentTrack.IsLoaded)
                return;

            audioPlayer.OutputVoice.SetVolume((float)(volumeSlider.Value / 100.0));
            audioPlayer.PlaySound(currentTrack);

            playButton.IsEnabled = false;
            stopButton.IsEnabled = true;

            await Dispatcher.InvokeAsync(async () =>
            {
                while (IsPlaying)
                {
                    currentTrack.Progress = audioPlayer.Progress * 800.0;
                    await Task.Delay(30);
                }

                currentTrack.Progress = 0;
                stopButton.IsEnabled = false;
                playButton.IsEnabled = true;
            });
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider && slider.Value <= 100.0 && slider.Value >= 0.0)
            {
                audioPlayer.OutputVoice.SetVolume((float)(slider.Value / 100.0));

                Config.Add("SoundVolume", (float)slider.Value);
                Config.Save();
            }
        }

        private void TracksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tracksListBox.SelectedItem == null)
                return;

            if (!IsPlaying)
            {
                if (tracksListBox.SelectedItem is SoundDataTrack currentTrack && currentTrack.IsLoaded)
                {
                    playButton.IsEnabled = true;
                }
                else
                {
                    playButton.IsEnabled = false;
                }
            }
        }

        public override void Closed()
        {
            audioPlayer.Dispose();
        }

        private void FrostySoundDataEditor_Loaded(object sender, RoutedEventArgs e)
        {
            if (bFirstTime)
            {
                audioPlayer = new AudioPlayer();

                List<SoundDataTrack> tracks = null;
                FrostyTaskWindow.Show("Loading tracks", "", owner =>
                {
                    tracks = InitialLoad(owner);
                });

                foreach (var track in tracks)
                    TracksList.Add(track);

                bFirstTime = false;
            }
        }

        protected virtual List<SoundDataTrack> InitialLoad(FrostyTaskWindow task)
        {
            return new List<SoundDataTrack>();
        }

        protected virtual Task ReloadTrack(NewWaveResource newWave, SoundDataTrack track)
        {
            return null;
        }

        private void SoundExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(tracksListBox.SelectedItem is SoundDataTrack track))
                return;

            FrostySaveFileDialog sfd = new FrostySaveFileDialog("Save WAV File", "WAV file (*.wav)|*.wav", "Sound", AssetEntry.Filename);

            if (!sfd.ShowDialog())
                return;

            for (int trackIndex = 0; trackIndex < tracksListBox.SelectedItems.Count; trackIndex++)
            {
                SoundDataTrack indexedTrack = (SoundDataTrack)tracksListBox.SelectedItems[trackIndex];
                String indexedFilename = sfd.FileName.Replace(".wav", " " + trackIndex + ".wav");
                SoundExportMenuItem_Export(indexedTrack, indexedFilename);
                logger.Log("Exported {0} to {1}", AssetEntry.Name, indexedFilename);
            }
        }

        private void SoundExportMenuItem_Export(SoundDataTrack track, String filename)
        {
            FrostyTaskWindow.Show("Exporting Sound", "", task =>
            {
                WAV.WAVFormatChunk fmt = new WAV.WAVFormatChunk(WAV.WAVFormatChunk.DataFormats.WAVE_FORMAT_PCM, (ushort)track.ChannelCount, (uint)track.SampleRate, (uint)(track.ChannelCount * 2 * track.SampleRate), (ushort)(2 * track.ChannelCount), 16);
                List<WAV.WAVDataFrame> frames = new List<WAV.WAVDataFrame>();

                for (int i = 0; i < track.Samples.Length / track.ChannelCount; i++)
                {
                    // write frame
                    WAV.WAV16BitDataFrame frame = new WAV.WAV16BitDataFrame((ushort)track.ChannelCount);
                    for (int channel = 0; channel < track.ChannelCount; channel++)
                    {
                        frame.Data[channel] = track.Samples[i * track.ChannelCount + channel];
                    }
                    frames.Add(frame);
                }

                WAV.WAVDataChunk data = new WAV.WAVDataChunk(fmt, frames);
                WAV.RIFFMainChunk main = new WAV.RIFFMainChunk(new WAV.RIFFChunkHeader(0, new byte[] { 0x52, 0x49, 0x46, 0x46 }, 0), new byte[] { 0x57, 0x41, 0x56, 0x45 });

                using (FileStream stream = new FileStream(filename, FileMode.Create))
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    main.Write(writer, new List<WAV.IRIFFChunk>(new WAV.IRIFFChunk[] { fmt, data }));
                }
            });

            logger.Log("Exported {0} to {1}", AssetEntry.Name, filename);
        }

        private void SoundImportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (tracksListBox.SelectedItem == null)
                return;

            FrostyOpenFileDialog ofd = new FrostyOpenFileDialog("Import Sound", "Audio Files (*.mp3; *.wav; *.opus; *.snr)|*.mp3; *.wav; *.opus; *.snr", "Sound");
            if (ofd.ShowDialog())
            {
                try
                {
                    FrostyTaskWindow.Show("Importing track", "", (task) =>
                    {
                        Dispatcher.Invoke(delegate
                        {
                            SoundDataTrack indexedTrack = (SoundDataTrack)tracksListBox.SelectedItem;
                            ImportSound(ofd.FileName, indexedTrack, task);
                        });
                    });
                }
                catch (Exception exp)
                {
                    App.AssetManager.RevertAsset(AssetEntry);
                    logger.LogError(exp.Message);
                }
            }
        }

        private string GetFormat(int? codec)
        {
            switch (codec)
            {
                case 2: return "s16b_int";
                case 3: return "eaxma";
                case 4: return "xas_int";
                case 5: return "ealayer3_int";
                case 6: return "ealayer3pcm_int";
                case 7: return "ealayer3spike_int";
                case 9: return "easpeex";
                case 11: return "eamp3";
                case 12: return "eaopus";
                case 13: return "eaatrac9";
                case 14: return "multistreamopus";
                case 15: return "multistreamopusuncoupled";
                default: return "multistreamopus";
            }
            ;
        }

        private static uint GetSegmentOffsetFlags(bool isValid, bool isStreaming)
        {
            if (isValid && isStreaming)
            {
                return 3;
            }
            if (isValid)
            {
                return 1;
            }
            return 0;
        }

        private static int AlignTo(int value, int alignment)
        {
            int remainder = value % alignment;
            if (remainder == 0)
            {
                return value;
            }
            int padding = alignment - remainder;
            return value + padding;
        }

        private async void ImportSound(string importFileName, SoundDataTrack track, FrostyTaskWindow task)
        {
            // The EA Tool only understands .wav and .mp3 natively.
            // Any other format (e.g. .opus, .snr, ...) is pre-converted to a temporary WAV via vgmstream.
            string tempWavFromVgmstream = null;
            string ext = Path.GetExtension(importFileName);
            bool needsVgmstream = !string.Equals(ext, ".wav", StringComparison.OrdinalIgnoreCase)
                               && !string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase);
            if (needsVgmstream)
            {
                tempWavFromVgmstream = await VgmStreamHelper.Instance.ConvertToWav(importFileName);
                if (tempWavFromVgmstream == null)
                    throw new Exception($"Failed to convert {ext} file to WAV via vgmstream. Make sure the file is a valid audio file supported by vgmstream.");
                importFileName = tempWavFromVgmstream;
            }

            try
            {

            string codec = GetFormat(track.CodecUnformatted);
            bool isSeekable = ((dynamic)Asset.RootObject).IsSeekable;
            bool shouldRemix = Config.Get<bool>("AutoRemixOnImport", true);

            (byte[] spsData, byte[] seekTableData) = await ToolHelper.Instance.ImportSound(importFileName, codec, isSeekable, track.ChannelCount, shouldRemix);

            bool hasStreamPool = ((PointerRef)((dynamic)base.Asset.RootObject).StreamPool).Type != PointerRefType.Null;
            byte[] chunkData;
            uint seekTableOffset, samplesOffset;
            dynamic originalSoundWave = RootObject;

            int chunkIndex = track.ChunkIndex;

            dynamic soundDataChunk = originalSoundWave.Chunks[track.ChunkIndex];
            ChunkAssetEntry existingChunkEntry = App.AssetManager.GetChunkEntry(track.ChunkId);
            NativeReader existingChunkData = new NativeReader(App.AssetManager.GetChunk(existingChunkEntry));

            // For assets with streampools, we want to add as few chunks as possible, so if there's already a chunk added for new tracks, use that
            bool hasExistingNewChunk = false;

            // As far as we know, no vanilla assets with streampools have more than one chunk,
            // so if the asset has more than one chunk, it is safe to assume the user has already added a new chunk from a previous import
            if (hasStreamPool && originalSoundWave.Chunks.Count > 1)
            {
                soundDataChunk = originalSoundWave.Chunks[originalSoundWave.Chunks.Count - 1];
                existingChunkEntry = App.AssetManager.GetChunkEntry(soundDataChunk.ChunkId);
                existingChunkData = new NativeReader(App.AssetManager.GetChunk(existingChunkEntry));

                chunkIndex = originalSoundWave.Chunks.Count - 1;

                hasExistingNewChunk = true;
            }

            // For streampool assets, new audio is always appended after the existing chunk data
            // so that other tracks' offsets remain valid. For non-streampool, a new standalone
            // chunk is created (offsetStart = 0).
            uint appendStart = (hasStreamPool || hasExistingNewChunk) ? (uint)existingChunkData.Length : 0u;

            if (seekTableData != null)
            {
                seekTableOffset = appendStart | GetSegmentOffsetFlags(isValid: true, hasStreamPool);
                int alignedSampleOffset = AlignTo((int)appendStart + seekTableData.Length, 4);
                chunkData = new byte[alignedSampleOffset - (int)appendStart + spsData.Length];
                Array.Copy(seekTableData, chunkData, seekTableData.Length);
                Array.Copy(spsData, 0, chunkData, alignedSampleOffset - (int)appendStart, spsData.Length);
                samplesOffset = (uint)alignedSampleOffset | GetSegmentOffsetFlags(isValid: true, hasStreamPool);
            }
            else
            {
                chunkData = spsData;
                samplesOffset = appendStart | GetSegmentOffsetFlags(isValid: true, hasStreamPool);
                seekTableOffset = 0 | GetSegmentOffsetFlags(isValid: false, hasStreamPool);
            }

            Guid newGuid = !hasStreamPool ? App.AssetManager.AddChunk(chunkData, CompressionType.None) : soundDataChunk.ChunkId;

            float durationInSeconds = ToolHelper.Instance.GetDurationInSecondsFromBuffer(spsData);

            NewWaveResource newWave = App.AssetManager.GetResAs<NewWaveResource>(App.AssetManager.GetResEntry(((string)originalSoundWave.Name).ToLower()));

            int index = 0;
            Dispatcher?.Invoke(() => { index = tracksListBox.SelectedIndex; });

            bool chunkIsAlreadyModified = existingChunkEntry != null && existingChunkEntry.IsAdded && track.ChunkId != null && !hasStreamPool;
            // For streampool we always reuse the existing soundDataChunk object so we can update its size in-place.
            // For non-streampool we create a new chunk object (unless the chunk was already added by a previous import).
            dynamic chunkToModify = (chunkIsAlreadyModified || hasStreamPool) ? soundDataChunk : Activator.CreateInstance(originalSoundWave.Chunks[0].GetType());

            if (hasStreamPool)
            {
                // Always append new audio after the existing chunk bytes so that every other track's
                // SamplesOffset still points to valid data. This covers both first and subsequent imports.
                using (MemoryStream ms = new MemoryStream())
                {
                    existingChunkData.Position = 0;
                    byte[] existingChunkBytes = existingChunkData.ReadToEnd();
                    ms.Write(existingChunkBytes, 0, existingChunkBytes.Length);
                    ms.Write(chunkData, 0, chunkData.Length);
                    App.AssetManager.ModifyChunk(newGuid, ms.ToArray(), CompressionType.None);
                    chunkToModify.ChunkSize = (uint)ms.Length;
                }
                // The existing chunk is already registered in the correct bundles — no changes needed.
            }
            else
            {
                chunkToModify.ChunkId = newGuid;
                chunkToModify.ChunkSize = (uint)chunkData.Length;
            }

            ChunkAssetEntry newAssetEntry = App.AssetManager.GetChunkEntry(newGuid);

            if (chunkIsAlreadyModified)
            {
                App.AssetManager.RevertAsset(existingChunkEntry);

                // add the new chunk to the existing superbundle
                foreach (var sb in existingChunkEntry.AddedSuperBundles)
                {
                    newAssetEntry.AddToSuperBundle(sb);
                }

                // add the new chunk to the existing bundles
                newAssetEntry.AddToBundles(existingChunkEntry.AddedBundles);
            }
            else if (!hasStreamPool && !hasExistingNewChunk)
            {
                // First non-streampool import: register the new chunk reference in the EBX.
                originalSoundWave.Chunks.Add(chunkToModify);
                chunkIndex = originalSoundWave.Chunks.Count - 1;

                // add the new chunk to the existing superbundle
                foreach (var sb in existingChunkEntry.SuperBundles)
                {
                    newAssetEntry.AddToSuperBundle(sb);
                }

                foreach (var sb in existingChunkEntry.AddedSuperBundles)
                {
                    newAssetEntry.AddToSuperBundle(sb);
                }

                // add the new chunk to the existing bundles
                newAssetEntry.AddToBundles(existingChunkEntry.Bundles);
                newAssetEntry.AddToBundles(existingChunkEntry.AddedBundles);
            }

            if (track.SegmentIndex > -1)
            {
                newWave.Segments[track.SegmentIndex].SamplesOffset = samplesOffset;
                newWave.Segments[track.SegmentIndex].SeekTableOffset = seekTableOffset;
                newWave.Segments[track.SegmentIndex].SegmentLength = durationInSeconds;
            }

            Dictionary<Guid, uint> uniqueChunks = new Dictionary<Guid, uint>();
            for (int i = 0; i < originalSoundWave.Chunks.Count; i++)
            {
                Guid guid = originalSoundWave.Chunks[i].ChunkId;
                if (!uniqueChunks.ContainsKey(guid))
                    uniqueChunks[guid] = originalSoundWave.Chunks[i].ChunkSize;
            }

            object chunks = newWave.Chunks;
            IList chunksList = (IList)chunks;
            chunksList.Clear();

            foreach (KeyValuePair<Guid, uint> item in uniqueChunks)
            {
                dynamic chunk = Activator.CreateInstance(newWave.Chunks.GetType().GetGenericArguments()[0]);
                chunk.ChunkId = item.Key;
                chunk.ChunkSize = Convert.ChangeType(item.Value, chunk.ChunkSize.GetType());
                chunksList.Add(chunk);
            }

            // Resolve the chunk index AFTER deduplication, by finding soundDataChunk's GUID
            // in the rebuilt list. This is correct even when originalSoundWave.Chunks contains
            // duplicate GUIDs (common in LocalizedWaveAssets), which collapse to fewer entries.
            if (track.VariationIndex > -1)
            {
                Guid targetGuid = (Guid)soundDataChunk.ChunkId;
                int rebuiltChunkIndex = 0;
                for (int i = 0; i < chunksList.Count; i++)
                {
                    if ((Guid)((dynamic)chunksList[i]).ChunkId == targetGuid)
                    {
                        rebuiltChunkIndex = i;
                        break;
                    }
                }

                if (hasStreamPool)
                    newWave.Variations[track.VariationIndex].StreamChunkIndex = (uint)rebuiltChunkIndex;
                else
                    newWave.Variations[track.VariationIndex].MemoryChunkIndex = (uint)rebuiltChunkIndex;
            }

            audioPlayer.Dispose();
            audioPlayer = new AudioPlayer();

            Dispatcher?.Invoke(() =>
            {
                Asset.Update();
                App.AssetManager.ModifyEbx(AssetEntry.Name, Asset);
                App.AssetManager.ModifyRes(((string)originalSoundWave.Name).ToLower(), newWave);

                App.AssetManager.GetResEntry(AssetEntry.Name).LinkAsset(App.AssetManager.GetChunkEntry(newGuid));

                EbxAssetEntry assetEntry = AssetEntry as EbxAssetEntry;
                assetEntry.LinkAsset(App.AssetManager.GetResEntry(AssetEntry.Name));

                // mark asset as modified and link the chunk
                AssetModified = true;
                InvokeOnAssetModified();

                Task reloadTrackData = ReloadTrack(newWave, track);
                if (reloadTrackData != null)
                {
                    reloadTrackData.RunSynchronously();
                }

                //List<SoundDataTrack> tracks = InitialLoad(task);

                //TracksList.Clear();
                //foreach (var theTrack in tracks)
                //    TracksList.Add(theTrack);
            });

            } // end try (opus pre-conversion guard)
            finally
            {
                if (tempWavFromVgmstream != null && File.Exists(tempWavFromVgmstream))
                    File.Delete(tempWavFromVgmstream);
            }
        }
    }
}
