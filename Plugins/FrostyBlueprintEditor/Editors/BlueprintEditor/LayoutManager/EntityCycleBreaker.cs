using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.Sugiyama;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.LayoutManager
{
    public class EntityCycleBreaker : CycleRemover
    {
        public override void DepthSearch(IConnection start)
        {
            if (VisitedNodes.Contains(start.Target.Node) && !(start.Target.RedirectNode != null && !VisitedNodes.Contains(start.Target.RedirectNode)))
            {
                Connections.Remove(start);
                
                // Don't redirect redirects!
                if (start.Target.Node is IRedirect || start.Source.Node is IRedirect)
                    return;

                if (GetConnections(start.Source.Node).Count > GetConnections(start.Target.Node).Count)
                {
                    if (start.Source is EntityPort port)
                    {
                        port.Redirect();
                    }
                }
                else
                {
                    if (start.Target is EntityPort port)
                    {
                        port.Redirect();
                    }
                }
                
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

        public EntityCycleBreaker(List<IConnection> connections) : base(connections)
        {
        }
    }
}