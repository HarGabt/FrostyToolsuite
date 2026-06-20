using System.Collections.Generic;
using System.Linq;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.Sugiyama;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms
{
    /// <summary>
    /// Sorts a graph topologically
    /// </summary>
    public class TopologicalSort : DepthFirstSearch
    {
        private List<IVertex> _vertices;
        private List<IVertex> _orderedNodes = new List<IVertex>();

        public override void DepthSearch(INode node)
        {
            if (VisitedNodes.Contains(node))
            {
                return;
            }
            
            VisitedNodes.Add(node);
            foreach (IConnection connection in GetConnections(node, PortDirection.In))
            {
                if (connection.Source.RedirectNode == null)
                {
                    DepthSearch(connection.Source.Node);
                }
                else
                {
                    DepthSearch(connection.Source.RedirectNode);
                }
            }

            if (!_orderedNodes.Contains(node))
            {
                _orderedNodes.Add(node);
            }
        }

        public List<IVertex> SortGraph()
        {
            CycleRemover cycleRemover = new CycleRemover(Connections);
            foreach (IVertex vertex in _vertices)
            {
                if (vertex is INode node)
                {
                    if (GetConnections(node, PortDirection.Out).Any())
                        continue;
                    
                    // If this node exclusively inputs, that means we can do a Depth search on it.
                    cycleRemover.RemoveCycles(node);
                    DepthSearch(node);
                }
            }

            return _orderedNodes;
        }

        public TopologicalSort(List<IVertex> vertices, List<IConnection> connections) : base(connections)
        {
            _vertices = vertices;
        }
    }
}