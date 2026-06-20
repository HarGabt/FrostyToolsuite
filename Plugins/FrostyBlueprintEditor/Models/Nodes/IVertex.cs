using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;

namespace BlueprintEditorPlugin.Models.Nodes
{
    /// <summary>
    /// Base implementation of an object which can be selected and moved in a graph editor.
    /// </summary>
    public interface IVertex : INotifyPropertyChanged
    {
        /// <summary>
        /// The location of the vertex on the graph
        /// </summary>
        Point Location { get; set; }
        
        /// <summary>
        /// The size of the vertex on the graph
        /// </summary>
        Size Size { get; set; }
        
        /// <summary>
        /// Whether or not the vertex is selected
        /// </summary>
        bool IsSelected { get; set; }
        
        /// <summary>
        /// The <see cref="INodeWrangler"/> this vertex is a part of
        /// </summary>
        INodeWrangler NodeWrangler { get; }

        /// <summary>
        /// Whether or not this vertex is valid. Will not be processed by <see cref="ExtensionsManager"/> if it is invalid
        /// </summary>
        /// <returns>True if it is valid, false otherwise</returns>
        bool IsValid();
        
        /// <summary>
        /// Occurs when adding the vertex onto the graph
        /// </summary>
        void OnCreation();
        
        /// <summary>
        /// Occurs when removing the vertex from the graph
        /// </summary>
        void OnDestruction();
    }
}