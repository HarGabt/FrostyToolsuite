using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.IO;
using BlueprintEditorPlugin.Models.Status;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Models.Nodes
{
    /// <summary>
    /// Base implementation of a transient item, which has it's own methods for saving into layouts.
    /// </summary>
    public interface ITransient : IVertex
    {
        /// <summary>
        /// This method is called whenever the Layout Manager tries to load a node of this type
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A bool on whether the operation was a success. If it wasn't, the node will not be added</returns>
        bool Load(LayoutReader reader);
        /// <summary>
        /// This saves the Transient into the layout.
        /// </summary>
        /// <param name="writer"></param>
        void Save(LayoutWriter writer);
    }
}