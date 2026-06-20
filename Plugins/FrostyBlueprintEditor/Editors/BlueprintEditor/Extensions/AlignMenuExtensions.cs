using System.Windows;
using BlueprintEditorPlugin.Models.Nodes;
using Frosty.Core;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Extensions
{
    public class AlignHorizontallyExtension : BlueprintMenuItemExtension
    {
        public override string SubLevelMenuName => "Alignment";
        public override string DisplayName => "Align Horizontally";

        public override RelayCommand ButtonClicked => new RelayCommand(o =>
        {
            double avg = 0.0;
            
            // Average out the horizontal position
            for (int i = 0; i < GraphEditor.NodeWrangler.SelectedVertices.Count; i++)
            {
                IVertex vertex = GraphEditor.NodeWrangler.SelectedVertices[i];
                if (i == 0)
                {
                    avg = vertex.Location.X;
                }
                else
                {
                    avg = (avg + vertex.Location.X) / 2;
                }
            }
            
            // Place them along it
            foreach (IVertex vertex in GraphEditor.NodeWrangler.SelectedVertices)
            {
                vertex.Location = new Point(avg, vertex.Location.Y);
            }
        });
    }
    
    public class AlignVerticallyExtension : BlueprintMenuItemExtension
    {
        public override string SubLevelMenuName => "Alignment";
        public override string DisplayName => "Align Vertically";

        public override RelayCommand ButtonClicked => new RelayCommand(o =>
        {
            double avg = 0.0;
            
            // Average out the vertical position
            for (int i = 0; i < GraphEditor.NodeWrangler.SelectedVertices.Count; i++)
            {
                IVertex vertex = GraphEditor.NodeWrangler.SelectedVertices[i];
                if (i == 0)
                {
                    avg = vertex.Location.Y;
                }
                else
                {
                    avg = (avg + vertex.Location.Y) / 2;
                }
            }
            
            // Place them along it
            foreach (IVertex vertex in GraphEditor.NodeWrangler.SelectedVertices)
            {
                vertex.Location = new Point(vertex.Location.X, avg);
            }
        });
    }
}