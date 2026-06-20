using System.Collections.Generic;
using System.Linq;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.Sugiyama;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using FrostyEditor;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.LayeredGraph
{
    public class LayerMaker : DepthFirstSearch
    {
        private List<IVertex> _vertices;
        private Dictionary<int, NodeLayer> _layers;

        private int _lvl = -1;
        private int _retries = 5;

        protected override void RemoveNode(INode node)
        {
            base.RemoveNode(node);
            _vertices.Remove(node);
        }

        /// <summary>
        /// This creates a list of <see cref="NodeLayer"/>s from the list of nodes and connections.
        /// </summary>
        /// <returns></returns>
        public List<NodeLayer> CreateLayers()
        {
            while (_vertices.Count != 0)
            {
                _lvl++;
            
                NodeLayer layer = new NodeLayer(new List<INode>());
                foreach (IVertex vertex in _vertices)
                {
                    // First we get only the Source nodes, as the first layer should only be sources
                    if (vertex is INode node)
                    {
                        if (GetConnections(node, PortDirection.In).Any())
                            continue;
                    
                        layer.Nodes.Add(node);
                    }
                }

                // An error has occured, brute force our solutions
                if (layer.Nodes.Count == 0)
                {
                    App.Logger.LogError("Oh no! Graph is no longer able to be broken down into layers, attempting brute force solution...");
                    CycleRemover remover = new CycleRemover(Connections);
                    foreach (IVertex vertex in _vertices)
                    {
                        if (vertex is INode node)
                        {
                            remover.RemoveCycles(node);
                        }
                    }

                    if (_retries == 0)
                    {
                        // Brute force our way through verts
                        foreach (IVertex vertex in _vertices)
                        {
                            if (vertex is INode node)
                            {
                                layer.Nodes.Add(node);
                            }
                        }
                        
                        App.Logger.LogError("Unable to layer this graph, Ran out of retries.");
                        
                        break;
                    }

                    _retries--;
                    _lvl--;
                    continue;
                }
                
                _layers.Add(_lvl, layer);
            
                foreach (INode node in _layers[_lvl].Nodes)
                {
                    RemoveNode(node);
                }
            }

            return _layers.Values.ToList();
        }

        public LayerMaker(List<IVertex> vertices, List<IConnection> connections) : base(connections)
        {
            _vertices = new List<IVertex>(vertices);
            Connections = new List<IConnection>(connections);
            
            _layers = new Dictionary<int, NodeLayer>();
        }
    }

    public struct NodeLayer
    {
        public List<INode> Nodes { get; }

        public double GetHeight()
        {
            if (Nodes.Count == 0)
                return 0.0;
            
            return Nodes.Max(node => node.Size.Height);
        }
        
        /// <summary>
        /// Gets the width of this layer.
        /// </summary>
        /// <returns>The height of the highest node in this layer</returns>
        public double GetWidth()
        {
            if (Nodes.Count == 0)
                return 0.0;
            
            return Nodes.Max(node => node.Size.Width);
        }
        
        public NodeLayer(List<INode> nodes)
        {
            Nodes = nodes;
        }

        public NodeLayer(INode node)
        {
            Nodes = new List<INode> { node };
        }
    }
}