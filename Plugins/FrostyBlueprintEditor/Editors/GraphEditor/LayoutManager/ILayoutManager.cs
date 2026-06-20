using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;

/* FORMAT STRUCTURE:
int - FormatVersion
 
int - TransientCount

NullTerminatedString - TypeHeader // Used to identify which Transient class to use for loading data
dynamic[] - TransientData // Transient handles all data saving.

int - VertexCount

int - VertId // The index of the vert in the INodeWrangler.Nodes list
Point - Location
Double - SizeX
Double - SizeY
*/
namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager
{
    /// <summary>
    /// Base implementation for a Layout manager
    /// </summary>
    public interface ILayoutManager
    {
        INodeWrangler NodeWrangler { get; set; }
        int Version { get; }

        /// <summary>
        /// Automatically sorts the Graph
        /// </summary>
        void SortLayout(bool optimized = false);

        /// <summary>
        /// This returns a bool on whether a layout exists or not.
        /// </summary>
        /// <param name="path">The path relative to the current layout path</param>
        /// <returns>Whether or not the operation was a success</returns>
        bool LayoutExists(string path);
        
        /// <summary>
        /// Loads a layout.
        /// </summary>
        /// <param name="path">The path to the layout. This is NOT relative to the current layout path</param>
        /// <returns></returns>
        bool LoadLayout(string path);

        /// <summary>
        /// Loads a layout in a relative path.
        /// </summary>
        /// <param name="path">The path relative to the current layout path</param>
        /// <returns>Whether or not the operation was a success</returns>
        bool LoadLayoutRelative(string path);

        /// <summary>
        /// Saves a layout.
        /// </summary>
        /// <param name="path">The path relative to the current layout path</param>
        /// <returns>Whether or not the operation was a success</returns>
        bool SaveLayout(string path);
    }
}