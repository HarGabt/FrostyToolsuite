using System;

namespace Frosty.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class RegisterTypeOverrideAttribute : Attribute
    {
        public string LookupName { get; set; }
        public Type EditorType { get; set; }
        public bool ApplyToChildClasses { get; set; }
        public int Priority { get; set; }
        public RegisterTypeOverrideAttribute(string lookupName, Type type, bool applyToChildClasses, int priority)
        {
            LookupName = lookupName;
            EditorType = type;
        }
    }
}
