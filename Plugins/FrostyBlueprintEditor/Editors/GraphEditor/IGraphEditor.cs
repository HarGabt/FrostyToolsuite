using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Status;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Editors.GraphEditor
{
    public interface IGraphEditor
    {
        INodeWrangler NodeWrangler { get; set; }
        ILayoutManager LayoutManager { get; set; }

        /// <summary>
        /// Executes when the GraphEditor has been closed
        /// </summary>
        void Closed();
    }
}