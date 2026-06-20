using System.IO;
using System.Windows;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.IO
{
    public class LayoutWriter : NativeWriter
    {
        public void Write(Point point)
        {
            Write(point.X);
            Write(point.Y);
        }

        public void Write(AssetClassGuid guid)
        {
            Write(guid.ExportedGuid);
            Write(guid.InternalId);
        }
        
        public LayoutWriter(Stream inStream, bool leaveOpen = false, bool wide = false) : base(inStream, leaveOpen, wide)
        {
        }
    }
}