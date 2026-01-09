// TODO: This class is currently unused for now.
// This class was created as it's ultimately a better method to filter out strings directly from properties, as we know which property is being read from when collecting a CString.
// Example: If the property being read is "DebugName" and the "EbxAsset" is of type x, then we can skip a particular cstring.

using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FrostyStringGeneration.Ebx
{
    public class EbxCStringReader
    {
        private const BindingFlags PropertyBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        private readonly HashSet<object> visited = new HashSet<object>();
        private List<string> cstringValues = new List<string>();

        public EbxCStringReader() {}

        public List<string> GetAllCStrings(EbxAsset asset)
        {
            visited.Clear();
            cstringValues.Clear();
            foreach (object obj in asset.Objects)
            {
                CollectCStringsFromObject(obj);
            }
            return cstringValues;
        }

        private void CollectCStringsFromObject(object obj)
        {
            if (obj == null)
            {
                return;
            }
            if (visited.Contains(obj))
            {
                return;
            }
            visited.Add(obj);
            Type objType = obj.GetType();
            PropertyInfo[] properties = objType.GetProperties(PropertyBindingFlags);
            Array.Sort(properties, new PropertyComparer());
            foreach (var prop in properties)
            {
                if (prop.GetCustomAttribute<IsTransientAttribute>() != null)
                {
                    continue;
                }
                object value;
                try
                {
                    value = prop.GetValue(obj);
                }
                catch
                {
                    continue;
                }
                if (value == null)
                {
                    continue;
                }
                CollectCStringsFromValue(value);
            }
        }

        private void CollectCStringsFromValue(object value)
        {
            Type fieldType = value.GetType();
            if (fieldType == typeof(CString))
            {
                cstringValues.Add(value.ToString());
                return;
            }
            if (fieldType.Name == "List`1")
            {
                int count = (int)fieldType.GetMethod("get_Count").Invoke(value, null);
                var itemGetter = fieldType.GetMethod("get_Item");
                for (int i = 0; i < count; i++)
                {
                    object listItem = itemGetter.Invoke(value, new object[] { i });
                    if (listItem != null)
                    {
                        CollectCStringsFromValue(listItem);
                    }
                }
                return;
            }
            if (fieldType.Namespace == "FrostySdk.Ebx" && fieldType.BaseType != typeof(Enum))
            {
                if (fieldType == typeof(PointerRef)
                    || fieldType == typeof(ResourceRef)
                    || fieldType == typeof(FileRef)
                    || fieldType == typeof(TypeRef)
                    || fieldType == typeof(BoxedValueRef))
                {
                    return;
                }
                CollectCStringsFromObject(value);
                return;
            }
        }

        internal class PropertyComparer : IComparer<PropertyInfo>
        {
            public int Compare(PropertyInfo x, PropertyInfo y)
            {
                return (x.MetadataToken < y.MetadataToken) ? -1 : 1;
            }
        }
    }
}
