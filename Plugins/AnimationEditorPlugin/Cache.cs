using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace AssetBankPlugin
{
    /// <summary>
    /// Caches all AntRefs and in which bundle they're stored.
    /// </summary>
    public class Cache
    {
        public static Dictionary<Guid, int> AntStateBundleIndices = new Dictionary<Guid, int>();
        public static Dictionary<Guid, Guid> AntRefMap = new Dictionary<Guid, Guid>();

        public static void ReadState(string path)
        {
            using (var s = File.OpenRead(path))
            {
                using (var r = new NativeReader(s))
                {
                    uint refCount = r.ReadUInt();

                    for (int i = 0; i < refCount; i++)
                    {
                        Guid guid = r.ReadGuid();
                        int index = r.ReadInt();
                        AntStateBundleIndices[guid] = index;
                    }
                }
            }
        }

        public static void WriteState(string path)
        {
            using (var s = File.OpenWrite(path))
            {
                using (var w = new NativeWriter(s))
                {
                    w.Write(AntStateBundleIndices.Count);

                    foreach (var i in AntStateBundleIndices)
                    {
                        w.Write(i.Key);
                        w.Write(i.Value);
                    }
                }
            }
        }

        public static void ReadMap(string path)
        {
            using (var s = File.OpenRead(path))
            {
                using (var r = new NativeReader(s))
                {
                    uint refCount = r.ReadUInt();

                    for (int i = 0; i < refCount; i++)
                    {
                        Guid guid = r.ReadGuid();
                        Guid index = r.ReadGuid();
                        AntRefMap[guid] = index;
                    }
                }
            }
        }

        public static void WriteMap(string path)
        {
            using (var s = File.OpenWrite(path))
            {
                using (var w = new NativeWriter(s))
                {
                    w.Write(AntRefMap.Count);

                    foreach (var i in AntRefMap)
                    {
                        w.Write(i.Key);
                        w.Write(i.Value);
                    }
                }
            }
        }
    }
}
