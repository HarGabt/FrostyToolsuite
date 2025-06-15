using FrostySdk.Ebx;
using FrostySdk.Managers.Entries;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Frosty.Core.Attributes
{
    /// <summary>
    /// This attribute registers the method for opening files in the Blueprint Editor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public class RegisterBlueprintEditorHandlerAttribute : Attribute
    {
        public Type classToHandle { get; set; }

        public RegisterBlueprintEditorHandlerAttribute(Type type)
        {
            classToHandle = type;
        }
    }

    public interface IBlueprintEditorHandler
    {
        void OpenAssetAsGraph(EbxAssetEntry asset);
    }
}
