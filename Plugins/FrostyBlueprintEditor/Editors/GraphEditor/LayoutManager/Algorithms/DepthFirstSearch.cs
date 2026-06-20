using System.Collections.Generic;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms
{
    /// <summary>
    /// A simple algorithm for implementing DFS, a way of searching down all connection paths.
    /// </summary>
    public abstract class DepthFirstSearch
    {
        public List<INode> VisitedNodes = new List<INode>();
        protected List<IConnection> Connections;
        
        protected List<IConnection> GetConnections(INode node)
        {
            List<IConnection> connections = new List<IConnection>();

            // Redirects need to be handled differently
            if (node is IRedirect)
            {
                foreach (IConnection connection in Connections)
                {
                    if (connection.Source.RedirectNode == node || connection.Target.RedirectNode == node)
                    {
                        connections.Add(connection);
                    }
                }
            }
            else
            {
                foreach (IConnection connection in Connections)
                {
                    if (connection.Source.Node == node || connection.Target.Node == node)
                    {
                        connections.Add(connection);
                    }
                }
            }

            return connections;
        }
        
        protected List<IConnection> GetConnections(INode node, PortDirection direction)
        {
            List<IConnection> connections = new List<IConnection>();
            
            // Redirects need to be handled differently
            if (node is IRedirect)
            {
                foreach (IConnection connection in Connections)
                {
                    if (connection.Target.RedirectNode == node && direction == PortDirection.In)
                    {
                        connections.Add(connection);
                    }

                    if (connection.Source.RedirectNode == node && direction == PortDirection.Out)
                    {
                        connections.Add(connection);
                    }
                }
            }
            else
            {
                foreach (IConnection connection in Connections)
                {
                    if (connection.Target.Node == node && direction == PortDirection.In && connection.Target.RedirectNode == null)
                    {
                        connections.Add(connection);
                    }

                    if (connection.Source.Node == node && direction == PortDirection.Out && connection.Source.RedirectNode == null)
                    {
                        connections.Add(connection);
                    }
                }
            }

            return connections;
        }

        protected virtual void RemoveNode(INode node)
        {
            VisitedNodes.Remove(node);
            foreach (IConnection connection in GetConnections(node, PortDirection.Out))
            {
                Connections.Remove(connection);
            }
        }
        
        public virtual void DepthSearch(INode node)
        {
            if (VisitedNodes.Contains(node))
            {
                return;
            }
            
            VisitedNodes.Add(node);
            foreach (IConnection connection in GetConnections(node, PortDirection.Out))
            {
                if (node is IRedirect)
                {
                    DepthSearch(connection.Target.RedirectNode);
                }
                else
                {
                    DepthSearch(connection.Target.Node);
                }
            }
        }

        public virtual void DepthSearch(IConnection start)
        {
            if (VisitedNodes.Contains(start.Target.Node) && !(start.Target.RedirectNode != null && !VisitedNodes.Contains(start.Target.RedirectNode)))
            {
                return;
            }
            
            if (start.Target.RedirectNode == null)
            {
                VisitedNodes.Add(start.Target.Node);
            }
            else
            {
                VisitedNodes.Add(start.Target.RedirectNode);
            }
            foreach (IConnection connection in GetConnections(start.Target.Node, PortDirection.Out))
            {
                DepthSearch(connection);
            }
        }

        public DepthFirstSearch(List<IConnection> connections)
        {
            Connections = connections;
        }
    }
}