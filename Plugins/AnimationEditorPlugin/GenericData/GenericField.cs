namespace AssetBankPlugin.GenericData
{
    public struct GenericField
    {
        public string Type;
        public uint TypeHash;
        public string Name;
        public int Offset;
        public object Data;
        public bool IsArray;
        public bool IsNative;
        public uint Size;
        public uint Alignment;

        public override string ToString()
        {
            string isArray = IsArray ? "[]" : "";
            return $"Field, \"{Name}\", {Type}{isArray}, {Offset}";
        }
    }
}
