using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SoundEditorPlugin.Helpers
{
    public abstract class HelperBase<T>
        where T : HelperBase<T>, new()
    {
        public enum InitializedState
        {
            NotInitialized,
            Initializing,
            Initialized
        };

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public InitializedState State = InitializedState.NotInitialized;
        public string BasePath { get; protected set; } = "Base";                 // The base directory to extract contents to
        public string ResourceName { get; protected set; } = "Resource";         // The resource name in Resources/ folder
        public string ToolName { get; protected set; } = "Tool";                 // Name of the extracted tool executable
        public string ResourcePath                                               // Full path to the extracted tool
        { 
            get 
            { 
                return Path.Combine(BasePath, ToolName); 
            } 
        }

        // Lazy Singleton pattern: https://csharpindepth.com/Articles/Singleton
        private static readonly Lazy<T> lazy = new Lazy<T>(() => new T());

        public static T Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        public async Task InitializeAsync()
        {
            await _semaphore.WaitAsync();
            State = InitializedState.Initializing;
            Directory.CreateDirectory(BasePath);

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                Stream entryStream = null;
                FileStream outputStream = null;

                try
                {
                    entryStream = entry.Open();
                    string outputPath = Path.Combine(BasePath, entry.Name);
                    if (!File.Exists(outputPath))
                    {
                        outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                        await entryStream.CopyToAsync(outputStream).ConfigureAwait(continueOnCapturedContext: false);
                    }
                }
                finally
                {
                    entryStream?.Dispose();
                    outputStream?.Dispose();
                }
            }

            State = InitializedState.Initialized;
            archive.Dispose();
            stream.Dispose();
            _semaphore.Release();
        }
    }
}
