using System.Collections.Generic;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Utilities;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms
{
    /// <summary>
    /// Gets an island from this node. Returns null if the island was already found
    /// </summary>
    public class IslandSolver : DepthFirstSearch
    {
        private VertexIsland _island;
        
        public override void DepthSearch(INode node)
        {
            if (VisitedNodes.Contains(node))
                return;
            
            VisitedNodes.Add(node);
            _island.Vertices.Add(node);
            
            foreach (IConnection connection in GetConnections(node))
            {
                // This hurt my head for some reason
                // Account for redirects
                if (connection.Target.RedirectNode != null)
                {
                    if (connection.Target.RedirectNode != node)
                    {
                        DepthSearch(connection.Target.RedirectNode);
                    }
                    else
                    {
                        DepthSearch(connection.Source.Node);
                    }
                }
                else
                {
                    if (connection.Target.Node != node)
                    {
                        DepthSearch(connection.Target.Node);
                    }
                    else
                    {
                        DepthSearch(connection.Source.Node);
                    }
                }
            }
        }

        public VertexIsland GetIsland(INode node)
        {
            // In another castle
            if (VisitedNodes.Contains(node))
                return null;
            
            _island = new VertexIsland();
            
            DepthSearch(node);

            return _island;
        }

        public IslandSolver(List<IConnection> connections) : base(connections)
        {
        }
    }

    public class VertexIsland
    {
        public List<IVertex> Vertices { get; }

        public VertexIsland()
        {
            Vertices = new List<IVertex>();
        }
    }
}