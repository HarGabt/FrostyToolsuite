using System.Collections.Generic;

namespace AssetBankPlugin.GenericData
{
    public class GenericClass
    {
        public string Name;
        public int Alignment;
        public int Size;
        public List<GenericField> Elements = new List<GenericField>();

        public GenericClass() { }

        public override string ToString()
        {
            return $"Class, \"{Name}\", Align {Alignment}, Size {Size}";
        }
    }
}
