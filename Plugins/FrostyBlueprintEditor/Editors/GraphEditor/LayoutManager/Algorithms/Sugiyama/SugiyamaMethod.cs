using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.LayeredGraph;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Options;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.Sugiyama
{
    public class SugiyamaMethod : IGraphAlgorithm
    {
        protected INodeWrangler _nodeWrangler;
        protected List<IConnection> _connections;
        protected List<IVertex> _vertices;

        protected List<NodeLayer> _layers = new List<NodeLayer>();
        protected List<VertexIsland> _islands = new List<VertexIsland>();

        protected List<IConnection> GetConnections(INode node)
        {
            List<IConnection> connections = new List<IConnection>();
            foreach (IConnection connection in _connections)
            {
                if (connection.Source.Node == node || connection.Target.Node == node)
                {
                    connections.Add(connection);
                }
            }

            return connections;
        }
        
        protected List<IConnection> GetConnections(INode node, PortDirection direction)
        {
            List<IConnection> connections = new List<IConnection>();
            foreach (IConnection connection in _connections)
            {
                if (connection.Target.Node == node && direction == PortDirection.In)
                {
                    connections.Add(connection);
                }

                if (connection.Source.Node == node && direction == PortDirection.Out)
                {
                    connections.Add(connection);
                }
            }

            return connections;
        }
        
        protected void RemoveLoops()
        {
            List<IConnection> enumeration = new List<IConnection>(_connections);
            foreach (IConnection connection in enumeration)
            {
                if (connection.Source.Node == connection.Target.Node)
                {
                    _connections.Remove(connection);
                }
            }
        }

        /// <summary>
        /// Gets rid of verts lacking connections
        /// </summary>
        protected void RemoveEmpty()
        {
            List<IVertex> verts = new List<IVertex>(_vertices);
            foreach (IVertex vertex in verts)
            {
                if (vertex is INode node)
                {
                    if (!GetConnections(node).Any())
                    {
                        _vertices.Remove(node);
                    }
                }
                else
                {
                    _vertices.Remove(vertex);
                }
            }
        }

        protected void AssignHorizontalPositions()
        {
            double current = 0.0;
            foreach (NodeLayer layer in _layers)
            {
                foreach (INode node in layer.Nodes)
                {
                    node.Location = new Point(current + (node.Size.Width * 0.5), 0);
                }

                current += EditorOptions.VertXSpacing + layer.GetWidth();
            }
        }

        protected void AssignVerticalPositions()
        {
            foreach (NodeLayer layer in _layers)
            {
                double current = 0.0;
                foreach (INode node in layer.Nodes)
                {
                    node.Location = new Point(node.Location.X, current);
                    current += (node.Size.Height) + EditorOptions.VertYSpacing;
                }
            }
        }
        
        protected int GetLayerFromNode(INode node)
        {
            for (var i = 0; i < _layers.Count; i++)
            {
                NodeLayer layer = _layers[i];
                if (layer.Nodes.Contains(node))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Move verts into different layers if their edge is too far away
        /// </summary>
        protected void MergeLayers()
        {
            for (var i = 0; i < _layers.Count; i++)
            {
                NodeLayer layer = _layers[i];
                List<INode> nodes = new List<INode>(layer.Nodes);
                foreach (INode node in nodes)
                {
                    int earliestLayer = 0;
                    foreach (IConnection connection in GetConnections(node, PortDirection.Out))
                    {
                        int targetLayer = GetLayerFromNode(connection.Target.Node);
                        
                        // This is on a much further layer
                        if (targetLayer > i + 1 && (targetLayer < earliestLayer || earliestLayer == 0))
                        {
                            earliestLayer = targetLayer;
                        }
                    }

                    // We found an earlier layer
                    if (earliestLayer != 0)
                    {
                        // We need to move our node as a result of this. The layer we move to should be the layer before the earliest
                        NodeLayer layerToMove = _layers[earliestLayer - 1];
                        layer.Nodes.Remove(node);
                        layerToMove.Nodes.Add(node);
                    }
                }
            }
        }

        public virtual void SortGraph()
        {
            // This will remove our loops and topologically sort the graph
            RemoveLoops();

            foreach (IVertex vertex in _vertices)
            {
                if (vertex is INode node)
                {
                    CycleRemover cycleBreaker = new CycleRemover(_connections);
                    cycleBreaker.RemoveCycles(node);
                }
            }
            
            TopologicalSort topologicalSort = new TopologicalSort(_vertices, _connections);
            List<IVertex> sortedVerts = topologicalSort.SortGraph();

            RemoveEmpty();
            RemoveLoops();
            
            // Create islands
            IslandSolver islandSolver = new IslandSolver(_connections);
            foreach (IVertex vertex in _vertices)
            {
                if (vertex is INode node)
                {
                    VertexIsland island = islandSolver.GetIsland(node);
                    if (island != null)
                    {
                        _islands.Add(island);
                    }
                }
            }

            LayerMaker layerMaker = new LayerMaker(sortedVerts, _connections);
            _layers = layerMaker.CreateLayers();
            
            MergeLayers();

            AssignHorizontalPositions();
            AssignVerticalPositions();
        }

        public SugiyamaMethod(List<IConnection> connections, List<IVertex> vertices)
        {
            _connections = connections;
            _vertices = vertices;
        }
    }
}