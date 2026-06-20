using System;
using BlueprintEditorPlugin.Editors.BlueprintEditor.LayoutManager;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager;

namespace BlueprintEditorPlugin.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RegisterEntityLayoutExtension : Attribute
    {
        public Type LayoutManagerType { get; set; }

        /// <summary>
        /// Registers a custom Graph Editor. Type must be an extension of <see cref="EntityLayoutManager"/>
        /// </summary>
        /// <param name="layoutManagerType"></param>
        public RegisterEntityLayoutExtension(Type layoutManagerType)
        {
            LayoutManagerType = layoutManagerType;
        }
    }
}