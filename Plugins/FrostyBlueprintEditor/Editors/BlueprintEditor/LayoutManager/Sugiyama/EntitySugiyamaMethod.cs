using System.Collections.Generic;
using System.Linq;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.LayeredGraph;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.Sugiyama;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Options;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.LayoutManager.Sugiyama
{
    public class EntitySugiyamaMethod : SugiyamaMethod
    {
        private INodeWrangler _nodeWrangler;
        
        public override void SortGraph()
        {
            // This will remove our loops and topologically sort the graph
            RemoveLoops();

            if (EditorOptions.RedirectCycles)
            {
                foreach (IVertex vertex in _vertices)
                {
                    if (vertex is INode node)
                    {
                        EntityCycleBreaker cycleBreaker = new(_connections);
                        cycleBreaker.RemoveCycles(node);
                    }
                }
            
                // Now that redirects have been made, we need to remake our lists
                _vertices = _nodeWrangler.Vertices.ToList();
                _connections = _nodeWrangler.Connections.ToList();
            }
            
            // Cycle breaker may not remove everything, so we pass over nodes yet again and remove cycles outright
            foreach (IVertex vertex in _vertices)
            {
                if (vertex is INode node)
                {
                    CycleRemover cycleBreaker = new(_connections);
                    cycleBreaker.RemoveCycles(node);
                }
            }
            
            TopologicalSort topologicalSort = new(_vertices, _connections);
            List<IVertex> sortedVerts = topologicalSort.SortGraph();

            RemoveEmpty();
            RemoveLoops();
            
            // Create islands
            IslandSolver islandSolver = new(_connections);
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

            LayerMaker layerMaker = new(sortedVerts, _connections);
            _layers = layerMaker.CreateLayers();
            
            MergeLayers();

            AssignHorizontalPositions();
            AssignVerticalPositions();
        }

        public EntitySugiyamaMethod(List<IConnection> connections, List<IVertex> vertices, INodeWrangler wrangler) : base(connections, vertices)
        {
            _nodeWrangler = wrangler;
        }
    }
}