using Frosty.Core.IO;
using FrostySdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Frosty.Core.Sdk.Bf2042
{
    public class Strings
    {
        public class ClassLookupHelper
        {
            public string name;
            public List<(Guid, string)> fieldNames = new List<(Guid, string)>();
        }
        
        public static Dictionary<uint, string> stringHash = new Dictionary<uint, string>();
        public static Dictionary<uint, string> classHash = new Dictionary<uint, string>();
        public static Dictionary<uint, Dictionary<uint, string>> fieldHash = new Dictionary<uint, Dictionary<uint, string>>();
        public static Dictionary<Guid, ClassLookupHelper> classGuidMap = new Dictionary<Guid, ClassLookupHelper>();
    }
    
    public class FieldMatcher
    {
        public static List<(int, int)> ComputeLCSAlignment(List<(Guid Type, string Name)> oldList, List<(Guid Type, string Name)> newList)
        {
            int n = oldList.Count;
            int m = newList.Count;
            int[,] dp = new int[n + 1, m + 1];

            // Build LCS DP table (using Type only)
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    if (oldList[i].Type == newList[j].Type)
                    {
                        dp[i + 1, j + 1] = dp[i, j] + 1;
                    }
                    else
                    {
                        dp[i + 1, j + 1] = Math.Max(dp[i, j + 1], dp[i + 1, j]);
                    }
                }
            }

            // Backtrack to recover alignment pairs
            int x = n, y = m;
            List<(int, int)> pairs = new List<(int, int)>();
            while (x > 0 && y > 0)
            {
                if (oldList[x - 1].Type == newList[y - 1].Type && dp[x, y] == dp[x - 1, y - 1] + 1)
                {
                    pairs.Add((x - 1, y - 1));
                    x -= 1;
                    y -= 1;
                }
                else if (dp[x - 1, y] >= dp[x, y - 1])
                {
                    x -= 1;
                }
                else
                {
                    y -= 1;
                }
            }

            pairs.Reverse();
            return pairs;
        }

        public static Dictionary<int, int> Match(List<(Guid Type, string Name)> oldList, List<(Guid Type, string Name)> newList)
        {
            var lcsPairs = ComputeLCSAlignment(oldList, newList);
            var mapping = new Dictionary<int, int>();
            var usedNew = new HashSet<int>();
            var anchors = new Dictionary<int, int>();
            var oldIndices = new List<int>();

            foreach (var (i, j) in lcsPairs)
            {
                anchors[i] = j;
                oldIndices.Add(i);
            }

            foreach (var (i, j) in lcsPairs)
            {
                Guid t = oldList[i].Type;

                // If type occurs only once in old -> direct match
                if (oldList.FindAll(x => x.Type == t).Count == 1)
                {
                    mapping[i] = j;
                    usedNew.Add(j);
                }
                else
                {
                    // Otherwise: resolve with context
                    int? prevAnchor = null;
                    int? nextAnchor = null;

                    foreach (var a in oldIndices)
                    {
                        if (a < i && (prevAnchor == null || a > prevAnchor)) prevAnchor = a;
                        if (a > i && (nextAnchor == null || a < nextAnchor)) nextAnchor = a;
                    }

                    int leftBound = prevAnchor.HasValue ? anchors[prevAnchor.Value] : -1;
                    int rightBound = nextAnchor.HasValue ? anchors[nextAnchor.Value] : newList.Count;

                    var candidates = new List<int>();
                    for (int k = leftBound + 1; k < rightBound; k++)
                    {
                        if (newList[k].Type == t && !usedNew.Contains(k))
                        {
                            candidates.Add(k);
                        }
                    }

                    int best = candidates.Count > 0 ? candidates[0] : j;

                    mapping[i] = best;
                    usedNew.Add(best);
                }
            }

            return mapping;
        }
    }
    
    public class TypeInfo : ClassesSdkCreator.TypeInfo
    {
        private bool m_hasNames = !ProfilesLibrary.IsLoaded(ProfileVersion.Battlefield2042, ProfileVersion.Battlefield6);

        private uint m_nameHash;
        private uint m_signature;
        public override void Read(MemoryReader reader)
        {
            if (m_hasNames)
            {
                Name = reader.ReadNullTerminatedString();
            }
            m_nameHash = reader.ReadUInt();

            Flags = reader.ReadUShort();
            Flags >>= 1;
            
            Size = reader.ReadUShort();

            Guid = reader.ReadGuid();

            long nameSpaceOffset = reader.ReadLong();
            ArrayTypeOffset = reader.ReadLong();

            Alignment = reader.ReadUShort();
            FieldCount = reader.ReadUShort();
            m_signature = reader.ReadUInt();

            long[] offsets = new long[7];
            for (int i = 0; i < 7; i++)
            {
                offsets[i] = reader.ReadLong();
            }

            reader.Position = nameSpaceOffset;
            NameSpace = reader.ReadNullTerminatedString();

            if (!m_hasNames)
            {
                if (Strings.classHash.ContainsKey(m_nameHash))
                {
                    Name = Strings.classHash[m_nameHash];
                }
                else if (Strings.stringHash.ContainsKey(m_nameHash))
                {
                    Name = Strings.stringHash[m_nameHash];
                }
                else if (Strings.classGuidMap.ContainsKey(Guid))
                {
                    Name = Strings.classGuidMap[Guid].name;
                }
                else
                {
                    if (Type == 2)
                    {
                        Name = "Struct_" + m_nameHash.ToString("x8");
                    }
                    else if (Type == 3)
                    {
                        Name = "Class_" + m_nameHash.ToString("x8");
                    }
                    else if (Type == 8)
                    {
                        Name = "Enum_" + m_nameHash.ToString("x8");
                    }
                    else if (Type == 0x1b)
                    {
                        Name = "Interface_" + m_nameHash.ToString("x8");
                    }
                    else if (Type == 0x1c)
                    {
                        Name = "Delegate_" + m_nameHash.ToString("x8");
                    }
                    else if (Type == 0x18)
                    {
                        Name = "Function_" + m_nameHash.ToString("x8");
                    }
                    else
                    {
                        Name = "Unknown_" + m_nameHash.ToString("x8");
                    }
                }
            }

            bool bReadFields = false;
            ParentClass = offsets[0];
            if (Type == 2 /* Structure */)
            {
                reader.Position = offsets[6];
                bReadFields = true;
            }
            else if (Type == 3 /* Class */)
            {
                reader.Position = offsets[1];
                bReadFields = true;
            }
            else if (Type == 8 /* Enum */)
            {
                ParentClass = 0;
                reader.Position = offsets[0];
                bReadFields = true;
            }
            else if (Type == 0x1c /* Delegate */)
            {
                ParentClass = 0;
                reader.Position = offsets[0];
                for (int i = 0; i < FieldCount; i++)
                {
                    ParameterInfo pi = new ParameterInfo();
                    pi.Read(reader);
                    Parameters.Add(pi);
                }
            }
            else if (Type == 0x18 /* Function */)
            {
                ParentClass = 0;
                reader.Position = offsets[5];
                for (int i = 0; i < FieldCount; i++)
                {
                    ParameterInfo pi = new ParameterInfo();
                    pi.Read(reader);
                    Parameters.Add(pi);
                }
            }

            if (bReadFields)
            {
                for (int i = 0; i < FieldCount; i++)
                {
                    FieldInfo fi = new FieldInfo();
                    fi.Read(reader, m_nameHash);
                    fi.Index = i;

                    Fields.Add(fi);
                }
                
                if (!m_hasNames && Strings.classGuidMap.ContainsKey(Guid))
                {
                    var classGuidHelper = Strings.classGuidMap[Guid];
                    List<(Guid, string)> newList = new List<(Guid, string)>();
                    foreach (var fieldInfo in Fields)
                    {
                        newList.Add((fieldInfo.TypeGuid, fieldInfo.Name));
                    }

                    var matching = FieldMatcher.Match(classGuidHelper.fieldNames, newList);
                    foreach (var pair in matching)
                    {
                        var newName = classGuidHelper.fieldNames[pair.Key].Item2;
                        if (!newName.StartsWith("Field_"))
                            Fields[pair.Value].Name = newName;
                    }
                }
            }
        }

        public override void Modify(DbObject classObj, Dictionary<long, ClassesSdkCreator.ClassInfo> offsetClassInfoMapping)
        {
            var arrayType = (offsetClassInfoMapping.ContainsKey(ArrayTypeOffset)) ? offsetClassInfoMapping[ArrayTypeOffset] : null;
            classObj.SetValue("nameHash", m_nameHash);

            if (arrayType != null)
            {
                classObj.SetValue("arrayNameHash", arrayType.TypeInfo.As<TypeInfo>().m_nameHash);
            }

            classObj.SetValue("signature", m_signature);
        }
    }

    public class ClassInfo : ClassesSdkCreator.ClassInfo
    {
        public override void Read(MemoryReader reader)
        {
            long thisOffset = reader.Position;
            long typeInfoOffset = reader.ReadLong();

            if (ProfilesLibrary.IsLoaded(ProfileVersion.Battlefield6))
            {
                Padding = new byte[] { reader.ReadByte(), reader.ReadByte() };
                IsDataContainer = reader.ReadUShort();
                Id = (ushort) reader.ReadUInt();
                ClassesSdkCreator.NextOffset = reader.ReadLong();
                long prevOffset = reader.ReadLong();

                ParentClass = reader.ReadLong();

                reader.Position = typeInfoOffset;

                TypeInfo = new TypeInfo();
                TypeInfo.Read(reader);

                if (TypeInfo.ParentClass != 0)
                {
                    ParentClass = TypeInfo.ParentClass;
                }

                if (ParentClass == thisOffset)
                {
                    ParentClass = 0;
                }
            }
            else
            {
                long prevOffset = reader.ReadLong();

                ClassesSdkCreator.NextOffset = reader.ReadLong();

                Id = reader.ReadUShort();
                IsDataContainer = reader.ReadUShort();
                Padding = new byte[] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte() };
                ParentClass = reader.ReadLong();

                reader.Position = typeInfoOffset;

                TypeInfo = new TypeInfo();
                TypeInfo.Read(reader);

                if (TypeInfo.ParentClass != 0)
                {
                    ParentClass = TypeInfo.ParentClass;
                }

                if (ParentClass == thisOffset)
                {
                    ParentClass = 0;
                }
            }
        }
    }

    public class FieldInfo : ClassesSdkCreator.FieldInfo
    {

        private bool m_hasNames = !ProfilesLibrary.IsLoaded(ProfileVersion.Anthem, ProfileVersion.Battlefield2042, ProfileVersion.Battlefield6);
        private uint m_nameHash;
        public void Read(MemoryReader reader, uint classHash)
        {
            if (m_hasNames)
            {
                Name = reader.ReadNullTerminatedString();
            }
            m_nameHash = reader.ReadUInt();
            if (!m_hasNames)
            {
                Name = "Field_" + m_nameHash.ToString("x8");
                if (Strings.fieldHash.ContainsKey(classHash))
                {
                    if (Strings.fieldHash[classHash].ContainsKey(m_nameHash))
                    {
                        Name = Strings.fieldHash[classHash][m_nameHash];
                    }
                    else if (Strings.stringHash.ContainsKey(m_nameHash))
                    {
                        Name = Strings.stringHash[m_nameHash];
                    }
                }
                else if (Strings.stringHash.ContainsKey(m_nameHash))
                {
                    Name = Strings.stringHash[m_nameHash];
                }
            }

            if (ProfilesLibrary.IsLoaded(ProfileVersion.Battlefield6))
            {
                Flags = reader.ReadUShort();
                Padding1 =  reader.ReadUShort();
                Offset = reader.ReadUInt();
                Padding1 = (ushort) reader.ReadUInt();
                TypeOffset = reader.ReadLong();
            }
            else
            {
                Flags = reader.ReadUShort();
                Offset = reader.ReadUShort();
                TypeOffset = reader.ReadLong();
            }
            
            long current = reader.Position;
            reader.Position = TypeOffset + 8;
            TypeGuid = reader.ReadGuid();
                
            reader.Position = current;
        }

        public override void Modify(DbObject fieldObj)
        {
            fieldObj.SetValue("nameHash", m_nameHash);
        }
    }

    public class ParameterInfo : ClassesSdkCreator.ParameterInfo
    {
        public override void Read(MemoryReader reader)
        {
            Name = reader.ReadNullTerminatedString();
            TypeOffset = reader.ReadLong();
            Type = reader.ReadLong();
            long defaultValueOffset = reader.ReadLong();
            if (defaultValueOffset == 0)
            {
                DefaultValue = null;
            }
            else
            {

            }
        }

        public override void Modify(DbObject fieldObj)
        {
        }
    }
}
