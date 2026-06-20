using System.Collections.Generic;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.Sugiyama;
using BlueprintEditorPlugin.Models.Connections;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.LayeredGraph
{
    /// <summary>
    /// A subtype of <see cref="CycleRemover"/> which reverses the connection after it is removed
    /// </summary>
    public class CycleBreaker : CycleRemover
    {
        public override void DepthSearch(IConnection start)
        {
            if (VisitedNodes.Contains(start.Target.Node) && !(start.Target.RedirectNode != null && !VisitedNodes.Contains(start.Target.RedirectNode)))
            {
                Connections.Remove(start);
                Connections.Add(new BaseConnection(start.Target, start.Source));
            }
            base.DepthSearch(start);
        }

        public CycleBreaker(List<IConnection> connections) : base(connections)
        {
        }
    }
}