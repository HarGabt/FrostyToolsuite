using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using FrostySdk.Managers.Entries;
using System.Collections;
using System.Diagnostics;
using System.Security.Principal;
using System.Linq;

namespace FrostySdk.IO
{
    public class EbxYamlWriter : IDisposable
    {
        private const BindingFlags PropertyBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        internal class PropertyComparer : IComparer<PropertyInfo>
        {
            public int Compare(PropertyInfo x, PropertyInfo y)
            {
                return (x.MetadataToken < y.MetadataToken) ? -1 : 1;
            }
        }

        private EbxAsset asset;
        private AssetManager am;
        private List<object> objs = new List<object>();
        private Stream stream;

        private int tabSize = 2;
        private bool writeOffsets;
        private string offsetKey;
        private readonly Stack<string> offsetKeyStack = new Stack<string>();
        private bool isLastLineEmpty = false;

        public EbxYamlWriter(EbxAsset inAsset, Stream inStream, AssetManager inAm, int inTabSize, bool inWriteOffsets)
        {
            asset = inAsset;
            am = inAm;
            stream = inStream;
            tabSize = inTabSize;
            writeOffsets = inWriteOffsets;
        }

        public void WriteObjects()
        {
            objs.Clear();
            objs.AddRange(asset.Objects);

            StringBuilder sb = new StringBuilder();

            if (writeOffsets)
            {
                sb.AppendLine("Offset(h)");
                sb.AppendLine("".PadLeft(8, '0') + "  File <" + asset.FileGuid.ToString() + "> : ");
            }
            else
            {
                sb.AppendLine("File <" + asset.FileGuid.ToString() + "> : ");
            }

            foreach (object obj in objs)
                sb.Append(ClassToYaml(obj, obj.GetType(), tabSize));
            RemoveEmptyLines(sb);
            string value = sb.ToString();
            byte[] valueBuffer = Encoding.UTF8.GetBytes(value);

            stream.Write(valueBuffer, 0, valueBuffer.Length);
        }


        private string GetYamlOffset(string suffix)
        {
            if (!writeOffsets || asset.OffsetsMap == null)
                return string.Empty;

            offsetKeyStack.Push(suffix);
            offsetKey += suffix;
            if (asset.OffsetsMap.TryGetValue(offsetKey, out long value))
            {
                string offset = value.ToString("X").PadLeft(8, '0');
                return offset + "  ";
            }
            return "".PadLeft(10);
        }

        private void PopYamlOffset()
        {
            if (!writeOffsets || asset.OffsetsMap == null)
                return;

            if (offsetKeyStack.Count > 0)
            {
                string suffix = offsetKeyStack.Pop();
                if (offsetKey.EndsWith(suffix))
                {
                    offsetKey = offsetKey.Substring(0, offsetKey.Length - suffix.Length);
                }
                else
                {
                    Trace.WriteLine($"Error: Suffix '{suffix}' does not match the current offsetKey.");
                }
            }
            else
            {
                Trace.WriteLine("Error: No suffix to pop from offsetKeyStack.");
            }
        }

        private string ClassToYaml(object Obj, Type ObjType, int TabCount = 0)
        {
            StringBuilder SB = new StringBuilder();
            PropertyInfo[] props = ObjType.GetProperties();
            int TotalCount = props.Length;

            PropertyInfo[] Properties = ObjType.GetProperties(PropertyBindingFlags);
            Array.Sort(Properties, new PropertyComparer());

            string InstanceGuid = "";
            FieldInfo FI = ObjType.GetField("__Guid", BindingFlags.NonPublic | BindingFlags.Instance);

            if (FI != null)
            {
                AssetClassGuid Guid = (AssetClassGuid)FI.GetValue(Obj);
                InstanceGuid = Guid.ToString();
            }

            string guidTag = string.IsNullOrEmpty(InstanceGuid) ? "" : $" <{InstanceGuid}>";
            if (TotalCount != 0 && (Properties.Length > 0 || (ObjType.BaseType != typeof(object) && ObjType.BaseType != typeof(ValueType))))
            {
                SB.AppendLine(GetYamlOffset(InstanceGuid) + "".PadLeft(TabCount) + ObjType.Name + guidTag + " : ");
                TabCount += tabSize;

                foreach (PropertyInfo PI in Properties)
                {
                    if (PI.GetCustomAttribute<IsTransientAttribute>() != null)
                        continue;

                    SB.Append(GetYamlOffset($"_{PI.Name}") + "".PadLeft(TabCount) + PI.Name + "[AddInfo] : ");

                    object Value = PI.GetValue(Obj);
                    string Tmp = "";

                    SB.Append(FieldToXml(Value, ref Tmp, TabCount));

                    SB.AppendLine();
                    SB = SB.Replace("[AddInfo]", Tmp);
                    PopYamlOffset();
                }

                TabCount -= tabSize;
                PopYamlOffset();
            }
            else
            {
                SB.AppendLine(GetYamlOffset(InstanceGuid) + "".PadLeft(TabCount) + ObjType.Name + guidTag + " :");
                PopYamlOffset();
            }

            return SB.ToString();
        }

        private string FieldToXml(object Value, ref string AdditionalInfo, int TabCount = 0)
        {
            Type FieldType = Value.GetType();
            StringBuilder SB = new StringBuilder();

            if (FieldType.Name == "List`1")
            {
                int Count = (int)FieldType.GetMethod("get_Count").Invoke(Value, null);
                AdditionalInfo = " [Count=" + Count + "]";

                if (Count > 0)
                {
                    SB.AppendLine();
                    TabCount += tabSize;

                    for (int i = 0; i < Count; i++)
                    {
                        SB.Append(GetYamlOffset($"_{i}") + "".PadLeft(TabCount) + "- member [" + i.ToString() + "] : ");

                        object SubValue = FieldType.GetMethod("get_Item").Invoke(Value, new object[] { i });
                        string Tmp = "";

                        SB.Append(FieldToXml(SubValue, ref Tmp, TabCount));

                        SB.AppendLine();
                        PopYamlOffset();
                    }

                    TabCount -= tabSize;
                    SB.Append("".PadLeft(TabCount));
                }
            }
            else
            {
                if (FieldType.Namespace == "FrostySdk.Ebx" && FieldType.BaseType != typeof(Enum))
                {
                    if (FieldType == typeof(CString)) SB.Append(Value.ToString());
                    else if (FieldType == typeof(ResourceRef)) SB.Append(Value.ToString());
                    else if (FieldType == typeof(FileRef)) SB.Append(Value.ToString());
                    else if (FieldType == typeof(TypeRef)) SB.Append(Value.ToString());
                    else if (FieldType == typeof(BoxedValueRef)) SB.Append(Value.ToString());
                    else if (FieldType == typeof(PointerRef))
                    {
                        PointerRef Reference = (PointerRef)Value;
                        if (Reference.Type == PointerRefType.Internal)
                        {
                            Type SubObjType = Reference.Internal.GetType();
                            AssetClassGuid guid = (AssetClassGuid)SubObjType.GetField("__Guid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Reference.Internal);
                            SB.Append("[" + SubObjType.Name + "] " + guid.ToString());
                        }
                        else if (Reference.Type == PointerRefType.External)
                        {
                            EbxAssetEntry entry = am.GetEbxEntry(Reference.External.FileGuid);
                            if (entry != null)
                            {
                                SB.Append("[Ebx] " + entry.Name + " [" + Reference.External.ClassGuid + "]");
                            }
                            else
                            {
                                SB.Append("[Ebx] BadRef " + Reference.External.FileGuid + "/" + Reference.External.ClassGuid);
                            }
                        }
                        else
                            SB.Append("nullptr");
                    }
                    else
                    {
                        TabCount += tabSize;
                        SB.AppendLine();

                        SB.Append(ClassToYaml(Value, Value.GetType(), TabCount));

                        TabCount -= tabSize;
                        SB.Append("".PadLeft(TabCount));
                    }
                }
                else
                {
                    if (FieldType == typeof(byte)) SB.Append(((byte)Value).ToString("X2"));
                    else if (FieldType == typeof(ushort)) SB.Append(((ushort)Value).ToString("X4"));
                    else if (FieldType == typeof(uint))
                    {
                        uint value = (uint)Value;
                        string val = Utils.GetString((int)value);

                        if (!val.StartsWith("0x"))
                        {
                            SB.Append(val + " [" + value.ToString("X8") + "]");
                        }
                        else
                        {
                            SB.Append(val);
                        }
                    }
                    else if (FieldType == typeof(int))
                    {
                        int value = (int)Value;
                        string val = Utils.GetString(value);

                        if (!val.StartsWith("0x"))
                        {
                            SB.Append(val + " [" + value.ToString("X8") + "]");
                        }
                        else
                        {
                            SB.Append(val);
                        }
                    }
                    else if (FieldType == typeof(ulong)) SB.Append(((ulong)Value).ToString("X16"));
                    else if (FieldType == typeof(float)) SB.Append(((float)Value).ToString());
                    else if (FieldType == typeof(double)) SB.Append(((double)Value).ToString());
                    else SB.Append(Value.ToString());
                }
            }

            return SB.ToString();
        }
        private void RemoveEmptyLines(StringBuilder sb)
        {
            StringBuilder result = new StringBuilder();
            int length = sb.Length;
            int lineStart = 0;

            for (int i = 0; i < length; i++)
            {
                if (sb[i] == '\n')
                {
                    int lineLength = i - lineStart;
                    // Check if the line contains non-whitespace characters
                    if (lineLength > 0 && !IsLineEmpty(sb, lineStart, lineLength))
                    {
                        // Copy the line to the result
                        result.Append(sb.ToString(lineStart, lineLength + 1)); // Include '\n'
                    }
                    lineStart = i + 1;
                }
            }

            if (lineStart < length && !IsLineEmpty(sb, lineStart, length - lineStart))
            {
                result.Append(sb.ToString(lineStart, length - lineStart));
            }

            sb.Clear();
            sb.Append(result);
        }

        private bool IsLineEmpty(StringBuilder sb, int start, int length)
        {
            for (int i = start; i < start + length; i++)
            {
                if (!char.IsWhiteSpace(sb[i]))
                    return false;
            }
            return true;
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stream copyOfStream = stream;
                stream = null;

                copyOfStream?.Close();
            }

            stream = null;
        }
    }
}
