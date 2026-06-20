using System;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;

namespace BlueprintEditorPlugin.Attributes
{
    /// <summary>
    /// This registers a <see cref="IEbxGraphEditor"/> to use for editing an asset
    /// The <see cref="IEbxGraphEditor"/> that is selected is based on the first one which is valid.
    ///
    /// <seealso cref="INode"/>
    /// <seealso cref="IConnection"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RegisterEbxGraphEditor : Attribute
    {
        public Type GraphType { get; set; }

        /// <summary>
        /// Registers a custom Graph Editor. Type must be an extension of <see cref="IEbxGraphEditor"/>
        /// </summary>
        /// <param name="graphEditorType"></param>
        public RegisterEbxGraphEditor(Type graphEditorType)
        {
            GraphType = graphEditorType;
        }
    }
}