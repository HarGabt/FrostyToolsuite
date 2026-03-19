using FrostySdk.Interfaces;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundEditorPlugin.Resources
{
    internal class NativeReader2 : NativeReader
    {
        public virtual bool KeepUnderlyingStreamOpen { get; set; }

        public NativeReader2(Stream inStream) : base(inStream)
        {
        }

        public NativeReader2(Stream inStream, IDeobfuscator inDeobfuscator)
            : base(inStream)
        { 
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stream copyOfStream = stream;
                stream = null;

                if (!KeepUnderlyingStreamOpen)
                {
                    copyOfStream?.Close();
                }
            }

            stream = null;
            buffer = null;
        }
    }
}
