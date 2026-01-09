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
        private bool isDebugInformationEnabled;

        public EbxYamlWriter(EbxAsset inAsset, Stream inStream, AssetManager inAm, int inTabSize, bool InIsDebugInformationEnabled)
        {
            asset = inAsset;
            am = inAm;
            stream = inStream;
            tabSize = inTabSize;
            isDebugInformationEnabled = InIsDebugInformationEnabled;
        }

        private string GetDebugOffset(string suffix)
        {
            if (!isDebugInformationEnabled || asset.DebugInformation == null)
                return string.Empty;

            long offset = asset.DebugInformation.GetDebugOffset(suffix);
            return " [Offset=0x" + offset.ToString("X").PadLeft(8, '0') + "]";
        }
        private void PopDebugOffset()
        {
            if (!isDebugInformationEnabled) return;

            asset.DebugInformation.PopDebugOffset();
        }

        public void WriteObjects()
        {
            objs.Clear();
            objs.AddRange(asset.Objects);
            asset.DebugInformation.ResetWorkingVariables();

            StringBuilder sb = new StringBuilder();

            sb.Append("File <" + asset.FileGuid.ToString() + "> :");

            foreach (object obj in objs)
                sb.Append(ClassToXml(obj, obj.GetType(), tabSize));
            string value = sb.ToString();
            byte[] valueBuffer = Encoding.UTF8.GetBytes(value);

            stream.Write(valueBuffer, 0, valueBuffer.Length);
        }


        private string ClassToXml(object Obj, Type ObjType, int TabCount = 0)
        {
            StringBuilder SB = new StringBuilder();
            PropertyInfo[] props = ObjType.GetProperties();
            int TotalCount = props.Length;

            PropertyInfo[] Properties = ObjType.GetProperties(PropertyBindingFlags);
            Array.Sort(Properties, new PropertyComparer());

            string StrGuid = "";
            string InstanceGuid = "";
            FieldInfo FI = ObjType.GetField("__Guid", BindingFlags.NonPublic | BindingFlags.Instance);

            if (FI != null)
            {
                AssetClassGuid Guid = (AssetClassGuid)FI.GetValue(Obj);
                StrGuid = " <" + Guid.ToString() + ">";
                InstanceGuid = Guid.ToString();
            }

            bool hasProperties = false;
            for (int i = 0; i < Properties.Length; i++)
            {
                if (Properties[i].GetCustomAttribute<IsTransientAttribute>() == null)
                {
                    hasProperties = true;
                    break;
                }

            }
                SB.AppendLine();
                SB.Append("".PadLeft(TabCount) + ObjType.Name + StrGuid + GetDebugOffset(InstanceGuid) + " : ");

                if (hasProperties)
                {
                    TabCount += tabSize;
                    foreach (PropertyInfo PI in Properties)
                    {
                        if (PI.GetCustomAttribute<IsTransientAttribute>() != null)
                            continue;

                        string propertyString = ("".PadLeft(TabCount) + PI.Name + "[AddInfo]" + GetDebugOffset($"_{PI.Name}") + " : ");
                        object Value = PI.GetValue(Obj);
                        string Tmp = "";

                        string fieldValue = FieldToXml(Value, ref Tmp, TabCount);

                        SB.AppendLine();
                        SB.Append(propertyString);
                        SB.Append(fieldValue);

                        SB = SB.Replace("[AddInfo]", Tmp);
                        PopDebugOffset();
                    }
                    TabCount -= tabSize;
                    PopDebugOffset();
                }
                else
                {
                    PopDebugOffset();
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

                    TabCount += tabSize;

                    for (int i = 0; i < Count; i++)
                    {
                        SB.AppendLine();
                        SB.Append("".PadLeft(TabCount) + "- member [" + i.ToString() + "]" + GetDebugOffset($"_{i}") + " : ");

                        object SubValue = FieldType.GetMethod("get_Item").Invoke(Value, new object[] { i });
                        string Tmp = "";

                        SB.Append(FieldToXml(SubValue, ref Tmp, TabCount));

                        PopDebugOffset();

                    }

                    TabCount -= tabSize;
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
                        SB.Append(ClassToXml(Value, Value.GetType(), TabCount));
                        TabCount -= tabSize;
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
