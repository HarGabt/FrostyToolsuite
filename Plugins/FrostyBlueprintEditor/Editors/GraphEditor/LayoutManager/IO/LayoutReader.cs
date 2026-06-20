using System;
using System.IO;
using System.Windows;
using FrostySdk.Ebx;
using FrostySdk.Interfaces;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.IO
{
    public class LayoutReader : NativeReader
    {
        public Point ReadPoint()
        {
            double x = ReadDouble();
            double y = ReadDouble();
            return new Point(x, y);
        }

        public AssetClassGuid ReadAssetClassGuid()
        {
            Guid exportedGuid = ReadGuid();
            int internalId = ReadInt();
            return new AssetClassGuid(exportedGuid, internalId);
        }
        
        public LayoutReader(Stream inStream) : base(inStream)
        {
        }

        public LayoutReader(Stream inStream, IDeobfuscator inDeobfuscator) : base(inStream, inDeobfuscator)
        {
        }
    }
}