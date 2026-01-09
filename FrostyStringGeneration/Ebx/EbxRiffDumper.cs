using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/*
 * This is a very lazy class in order to dump CStrings directly, very quickly, without fully parsing the ebx definition or creating the ebx object from reflection
 */
namespace FrostyStringGeneration.Ebx
{
    internal enum EbxVersion
    {
        Version2 = 0x0FB2D1CE,
        Version4 = 0x0FB4D1CE,
        Version6 = 0x46464952 // RIFF LE
    }

    internal enum RiffEbxSection
    {
        EBX = 0x45425800, // BE
        EBXS = 0x45425853, // BE
        EBXD = 0x45425844, // BE
        EFIX = 0x45464958, // BE
        EBXX = 0x45425858, // BE
        REFL = 0x5245464C  // BE
    }

    internal class EbxRiffDumper
    {
        private List<uint> dataOffsets = new List<uint>();
        private List<uint> pointerOffsets = new List<uint>();
        private List<uint> resourceRefOffsets = new List<uint>();
        private List<uint> importOffsets = new List<uint>();
        private List<uint> typeInfoOffsets = new List<uint>();

        internal Guid fileGuid;
        internal uint exportedCount; 
        internal long arraysOffset;
        internal List<Guid> classGuids = new List<Guid>();
        internal List<EbxInstance> instances = new List<EbxInstance>();
        internal List<EbxArray> arrays = new List<EbxArray>();
        internal List<EbxBoxedValue> boxedValues = new List<EbxBoxedValue>();
        internal List<EbxImportReference> imports = new List<EbxImportReference>();
        internal long boxedValuesOffset;
        internal long stringsOffset;
        internal uint arrayCount;
        internal uint boxedValuesCount;

        private byte[] EFIX = new byte[4] { 0x45, 0x46, 0x49, 0x58 };
        private long dataStartOffset;

        public EbxRiffDumper(NativeReader reader)
        {
            reader.Position = 0;

            // RIFF
            var magic = (EbxVersion)reader.ReadUInt();
            if (magic != EbxVersion.Version6)
                throw new InvalidDataException("Ebx not in RIFF format."); // RIFX and LIST
            uint size = reader.ReadUInt();

            // EBX
            uint ebxHeader = reader.ReadUInt(Endian.Big);
            if (ebxHeader != (uint)RiffEbxSection.EBX && ebxHeader != (uint)RiffEbxSection.EBXS) // EBXS ????
                throw new InvalidDataException("Not valid EBX/EBXS.");

            // EBXD
            if (reader.ReadUInt(Endian.Big) != (uint)RiffEbxSection.EBXD)
            {
                throw new InvalidDataException("Not valid EBXD chunk.");
            }

            uint ebxdSize = reader.ReadUInt();

            long ebxdOffset = reader.Position;

            reader.Pad(16);

            dataStartOffset = reader.Position;

            reader.Position = ebxdOffset + ebxdSize;

            reader.Pad(2);

            // EFIX
            if (reader.ReadUInt(Endian.Big) != (uint)RiffEbxSection.EFIX)
                throw new InvalidDataException("Not valid EFIX chunk.");
            uint efixSize = reader.ReadUInt();

            long efixOffset = reader.Position;

            fileGuid = reader.ReadGuid();
            uint classGuidCount = reader.ReadUInt();

            for (int i = 0; i < classGuidCount; i++)
            {
                classGuids.Add(reader.ReadGuid());
            }

            uint signatureCount = reader.ReadUInt();

            for (int i = 0; i < signatureCount; i++)
            {
                byte[] signature = reader.ReadBytes(4);
                Guid classGuid = classGuids[i];
                byte[] classGuidBytes = classGuid.ToByteArray();

                byte[] typeInfoGuidByteArray = new byte[16];
                Array.Copy(classGuidBytes, 4, typeInfoGuidByteArray, 0, 12);
                Array.Copy(signature, 0, typeInfoGuidByteArray, 12, 4);

                Guid typeInfoGuid = new Guid(typeInfoGuidByteArray);
                classGuids[i] = typeInfoGuid;
            }

            exportedCount = reader.ReadUInt();
            uint dataOffsetCount = reader.ReadUInt();

            for (int i = 0; i < dataOffsetCount; i++)
            {
                uint offset = reader.ReadUInt();
                dataOffsets.Add(offset);

                long curPos = reader.Position;
                reader.Position = dataStartOffset + offset;

                instances.Add
                (
                    new EbxInstance
                    {
                        ClassRef = reader.ReadUShort(),
                        Count = 1,
                        IsExported = (i < exportedCount)
                    }
                );

                reader.Position = curPos;
            }

            uint pointerOffsetCount = reader.ReadUInt();
            for (int i = 0; i < pointerOffsetCount; i++)
            {
                pointerOffsets.Add(reader.ReadUInt());
            }

            uint resourceRefOffsetCount = reader.ReadUInt();
            for (int i = 0; i < resourceRefOffsetCount; i++)
            {
                resourceRefOffsets.Add(reader.ReadUInt());
            }

            uint importReferenceCount = reader.ReadUInt();
            for (int i = 0; i < importReferenceCount; i++)
            {
                imports.Add
                (
                    new EbxImportReference
                    {
                        FileGuid = reader.ReadGuid(),
                        ClassGuid = reader.ReadGuid()
                    }
                );
            }

            uint importOffsetCount = reader.ReadUInt();
            for (int i = 0; i < importOffsetCount; i++)
            {
                importOffsets.Add(reader.ReadUInt());
            }

            uint typeInfoOffsetCount = reader.ReadUInt();
            for (int i = 0; i < typeInfoOffsetCount; i++)
            {
                typeInfoOffsets.Add(reader.ReadUInt());
            }

            arraysOffset = reader.ReadUInt();
            boxedValuesOffset = reader.ReadUInt();
            stringsOffset = reader.ReadUInt() + dataStartOffset;

            if (reader.Position != efixOffset + efixSize)
            {
                reader.Position = efixOffset + efixSize;
            }

            // EBXX
            if (reader.ReadUInt(Endian.Big) != (uint)RiffEbxSection.EBXX)
                throw new InvalidDataException("Not valid EBXX chunk.");
            uint ebxxSize = reader.ReadUInt();
            arrayCount = reader.ReadUInt();
            boxedValuesCount = reader.ReadUInt();

            for (int i = 0; i < arrayCount; i++)
            {
                uint offset = reader.ReadUInt();
                uint count = reader.ReadUInt();
                reader.Position += 6;
                ushort classRef = reader.ReadUShort();

                arrays.Add
                (
                    new EbxArray
                    {
                        Offset = offset,
                        Count = count,
                        ClassRef = classRef
                    }
                );
            }

            for (var i = 0; i < boxedValuesCount; i++)
            {
                uint offset = reader.ReadUInt();
                uint count = reader.ReadUInt();
                uint hash = reader.ReadUInt();
                ushort type = reader.ReadUShort();
                ushort classRef = reader.ReadUShort();

                boxedValues.Add
                (
                    new EbxBoxedValue
                    {
                        Offset = offset,
                        Type = type,
                        ClassRef = classRef
                    }
                );
            }

            reader.Position = dataStartOffset;
        }

        public List<string> DumpCStrings(NativeReader reader)
        {
            reader.Position = stringsOffset;

            List<string> CStrings = new List<string>();
            StringBuilder sb = new StringBuilder();

            while (reader.Position < reader.Length)
            {
                byte currentByte = reader.ReadByte();

                if (currentByte == 0)
                {
                    string currentString = sb.ToString();
                    if (!string.IsNullOrEmpty(currentString))
                    {
                        CStrings.Add(sb.ToString());
                    }
                    sb.Clear();
                }
                else
                {
                    sb.Append((char)currentByte);
                }

                if (CheckForEFIX(reader, currentByte))
                {
                    break;
                }
            }

            return CStrings;
        }
        private bool CheckForEFIX(NativeReader reader, byte firstByte)
        {
            byte[] buffer = new byte[EFIX.Length];
            buffer[0] = firstByte;

            long currentPos = reader.Position;

            for (int i = 1; i < EFIX.Length; i++)
            {
                if (reader.Position >= reader.Length)
                {
                    reader.Position = currentPos;
                    return false;
                }
                buffer[i] = reader.ReadByte();

            }

            reader.Position = currentPos;

            return buffer.SequenceEqual(EFIX);

        }
    }
}
