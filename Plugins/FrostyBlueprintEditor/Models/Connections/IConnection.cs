using System.ComponentModel;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;

namespace BlueprintEditorPlugin.Models.Connections
{
    /// <summary>
    /// Base implementation for a Connection between 2 <see cref="IPort"/>s on <see cref="INode"/>s
    ///
    /// <seealso cref="IPort"/>
    /// <seealso cref="INode"/>
    /// <seealso cref="IGraphEditor"/>
    /// </summary>
    public interface IConnection : INotifyPropertyChanged
    {
        IPort Source { get; set; }
        IPort Target { get; set; }
        bool IsSelected { get; set; }
    }
}