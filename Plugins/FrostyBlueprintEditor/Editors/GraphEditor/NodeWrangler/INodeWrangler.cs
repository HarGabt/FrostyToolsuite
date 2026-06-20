using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Connections.Pending;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler
{
    public interface INodeWrangler
    {
        ObservableCollection<IVertex> Vertices { get; }
        ObservableCollection<IVertex> SelectedVertices { get; }
        ObservableCollection<IConnection> Connections { get; }
        IPendingConnection PendingConnection { get; }
        ICommand RemoveConnectionsCommand { get; }

        void AddVertex(IVertex vertex);
        void RemoveVertex(IVertex vertex);

        void AddConnection(IConnection connection);
        void RemoveConnection(IConnection connection);

        void ClearConnections(INode node);
        void ClearConnections(INode node, PortDirection direction);
        void ClearConnections(IPort port);

        IEnumerable<IConnection> GetConnections(INode node);
        IEnumerable<IConnection> GetConnections(INode node, PortDirection direction);
        IEnumerable<IConnection> GetConnections(IPort port);
    }
}