using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Connections.Pending;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using Frosty.Core;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler
{
    public abstract class BaseNodeWrangler : INodeWrangler
    {
        public ObservableCollection<IVertex> Vertices { get; protected set; } = new ObservableCollection<IVertex>();
        public ObservableCollection<IVertex> SelectedVertices { get; protected set; } = new ObservableCollection<IVertex>();
        public ObservableCollection<IConnection> Connections { get; protected set; } = new ObservableCollection<IConnection>();
        
        public IPendingConnection PendingConnection { get; protected set; }
        public ICommand RemoveConnectionsCommand { get; protected set; }

        #region Node

        public virtual void AddVertex(IVertex vertex)
        {
            // TODO: Stupid threading bullshit won't let me access this because it's on a UI thread
            // Asshole!
            Application.Current.Dispatcher.Invoke(() =>
            {
                Vertices.Add(vertex);
            });
        }

        public virtual void RemoveVertex(IVertex vertex)
        {
            if (vertex is INode node && !(vertex is IRedirect))
            {
                ClearConnections(node);
            }

            vertex.OnDestruction();
            
            // TODO: Stupid threading bullshit won't let me access this because it's on a UI thread
            // Asshole!
            Application.Current.Dispatcher.Invoke(() =>
            {
                Vertices.Remove(vertex);
            });
        }

        #endregion

        #region Connection Editing

        public virtual void AddConnection(IConnection connection)
        {
            // TODO: Stupid threading bullshit won't let me access this because it's on a UI thread
            // Asshole!
            Application.Current.Dispatcher.Invoke(() =>
            {
                Connections.Add(connection);
            });
        }

        public virtual void RemoveConnection(IConnection connection)
        {
            if (GetConnections(connection.Source).FirstOrDefault() == null)
            {
                connection.Source.IsConnected = false;
            }

            if (GetConnections(connection.Target).FirstOrDefault() == null)
            {
                connection.Target.IsConnected = false;
            }

            // TODO: Stupid threading bullshit won't let me access this because it's on a UI thread
            // Asshole!
            Application.Current.Dispatcher.Invoke(() =>
            {
                Connections.Remove(connection);
            });
            
            connection.Source.Node.OnOutputUpdated(connection.Source);
            connection.Target.Node.OnInputUpdated(connection.Target);
        }

        public virtual void ClearConnections(INode node)
        {
            if (node is IRedirect)
            {
                App.Logger.LogError("Cannot clear connections of IRedirects");
                return;
            }
            
            List<IConnection> connections = GetConnections(node).ToList();
            foreach (IConnection connection in connections)
            {
                RemoveConnection(connection);
            }
        }
        
        public virtual void ClearConnections(INode node, PortDirection direction)
        {
            if (node is IRedirect)
            {
                App.Logger.LogError("Cannot clear connections of IRedirects");
                return;
            }
            
            List<IConnection> connections = GetConnections(node).ToList();
            foreach (IConnection connection in connections)
            {
                if (direction == PortDirection.In && connection.Target.Node == node)
                {
                    RemoveConnection(connection);
                }
                else if (direction == PortDirection.Out && connection.Source.Node == node)
                {
                    RemoveConnection(connection);
                }
            }
        }
        
        public virtual void ClearConnections(IPort port)
        {
            if (port.Node is IRedirect)
            {
                App.Logger.LogError("Cannot clear connections of IRedirects");
                return;
            }
            
            List<IConnection> connections = GetConnections(port).ToList();
            foreach (IConnection connection in connections)
            {
                if (connection.Source.Node is IRedirect || connection.Target.Node is IRedirect)
                    continue;
                
                RemoveConnection(connection);
            }
        }

        #endregion

        #region Getting Connections

        public virtual IEnumerable<IConnection> GetConnections(INode node)
        {
            foreach (IConnection connection in Connections)
            {
                if (connection.Source.Node == node || connection.Target.Node == node)
                {
                    yield return connection;
                }
            }
        }
        
        public virtual IEnumerable<IConnection> GetConnections(INode node, PortDirection direction)
        {
            foreach (IConnection connection in Connections)
            {
                if ((connection.Source.Node == node && direction == PortDirection.Out) || (connection.Target.Node == node && direction == PortDirection.In))
                {
                    yield return connection;
                }
            }
        }

        public virtual IEnumerable<IConnection> GetConnections(IPort port)
        {
            foreach (IConnection connection in Connections)
            {
                if (connection.Source == port || connection.Target == port)
                {
                    yield return connection;
                }
            }
        }

        #endregion
        
        public BaseNodeWrangler()
        {
            PendingConnection = new BasePendingConnection(this);
            RemoveConnectionsCommand = new DelegateCommand<IPort>(ClearConnections);
        }
    }
}