using Frosty.Controls;
using FrostySdk.IO;

namespace AssetBankPlugin.GenericData
{
    public abstract class Section
    {
        public abstract Endian Endianness { get; set; }
        public abstract uint DataSize { get; set; }
        public abstract uint DataOffset { get; set; }

        /// <summary>
        /// Reads a <see cref="Section"/> of an AntPackage asset bank and returns it.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Section ReadSection(NativeReader r)
        {
            // Read type and go back to the beginning of the block.
            string blockType = r.ReadSizedString(7);
            Endian endian = r.ReadSizedString(1) == "b" ? Endian.Big : Endian.Little;
            r.BaseStream.Position -= 8;

            // Read the section.
            Section result = null;
            switch (blockType)
            {
                case SectionStrm.Identifier:
                    result = new SectionStrm(r, endian);
                    break;
                case SectionRefl.Identifier:
                    result = new SectionRefl(r, endian);
                    break;
                case SectionData.Identifier:
                    result = new SectionData(r, endian);
                    break;
                default:
                    FrostyMessageBox.Show($"Unknown BlockType: {blockType}");
                    break;
            }

            return result;
        }
    }
}
