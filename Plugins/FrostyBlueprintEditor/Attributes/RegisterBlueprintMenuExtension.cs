using System;
using BlueprintEditorPlugin.Editors.BlueprintEditor;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Extensions;

namespace BlueprintEditorPlugin.Attributes
{
    /// <summary>
    /// Registers a menu extension for the <see cref="BlueprintGraphEditor"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RegisterBlueprintMenuExtension : Attribute
    {
        public Type MenuType { get; set; }

        /// <summary>
        /// Registers a custom Graph Editor. Type must be an extension of <see cref="BlueprintMenuItemExtension"/>
        /// </summary>
        /// <param name="menuType"></param>
        public RegisterBlueprintMenuExtension(Type menuType)
        {
            MenuType = menuType;
        }
    }
}