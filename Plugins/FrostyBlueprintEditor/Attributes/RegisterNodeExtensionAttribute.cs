using System;
using BlueprintEditorPlugin.Editors.BlueprintEditor;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;

namespace BlueprintEditorPlugin.Attributes
{
    /// <summary>
    /// This registers a Type mapping for the <see cref="BlueprintGraphEditor"/>.
    /// This will override any mappings found within the Blueprint Editor's core if this one is valid, be wise!
    ///
    /// <seealso cref="EntityNode"/>
    /// <seealso cref="EntityConnection"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RegisterEntityNode : Attribute
    {
        public Type EntityNodeExtension { get; set; }

        /// <summary>
        /// Registers a mapping for an entity node. 
        /// </summary>
        /// <param name="entityNodeExtension">type of the extension. Must be a subclass of <see cref="EntityNode"/></param>
        public RegisterEntityNode(Type entityNodeExtension)
        {
            EntityNodeExtension = entityNodeExtension;
        }
    }
}