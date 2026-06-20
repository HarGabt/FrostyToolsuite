using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Options;
using Frosty.Core;
using Frosty.Core.Windows;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Extensions
{
    public class DistributeHorizontallyMenuExtension : BlueprintMenuItemExtension
    {
        public override string SubLevelMenuName => "Distribution";
        public override string DisplayName => "Distribute horizontally";
        public override string ToolTip => "This will distribute the selected nodes evenly on the horizontal axis";

        public override RelayCommand ButtonClicked => new RelayCommand(o =>
        {
            FrostyTaskWindow.Show("Distributing...", "", task =>
            {
                // TODO: Use an implementation of Depth First Search for distributing instead
                // Doing it based on topological sort means that 2 nodes with the same source will be conducted sequentially
                // Making one come after another, instead we should organize them seperately both based on the source(and not eachother)
                TopologicalSort sort = new TopologicalSort(GraphEditor.NodeWrangler.SelectedVertices.ToList(), GraphEditor.NodeWrangler.Connections.ToList());
                List<IVertex> sortedVerts = sort.SortGraph();

                for (int i = 1; i < sortedVerts.Count; i++)
                {
                    IVertex previous = sortedVerts[i - 1];
                    IVertex current = sortedVerts[i];
                
                    // Don't bother not doing selected verts
                    if (!GraphEditor.NodeWrangler.SelectedVertices.Contains(current))
                        continue;

                    current.Location = new Point(previous.Location.X + previous.Size.Width + EditorOptions.VertXSpacing, current.Location.Y);
                }
            });
        });
    }
    
    public class DistributeVerticallyMenuExtension : BlueprintMenuItemExtension
    {
        public override string SubLevelMenuName => "Distribution";
        public override string DisplayName => "Distribute vertically";
        public override string ToolTip => "This will distribute the selected nodes evenly on the vertically axis";

        public override RelayCommand ButtonClicked => new RelayCommand(o =>
        {
            FrostyTaskWindow.Show("Distributing...", "", task =>
            {
                // TODO: Sort the list from lowest position to highest
                List<IVertex> sortedVerts = GraphEditor.NodeWrangler.SelectedVertices.ToList();

                for (int i = 1; i < sortedVerts.Count; i++)
                {
                    IVertex previous = sortedVerts[i - 1];
                    IVertex current = sortedVerts[i];
                
                    // Don't bother not doing selected verts
                    if (!GraphEditor.NodeWrangler.SelectedVertices.Contains(current))
                        continue;

                    current.Location = new Point(current.Location.X, previous.Location.Y + previous.Size.Height + EditorOptions.VertYSpacing);
                }
            });
        });
    }
}