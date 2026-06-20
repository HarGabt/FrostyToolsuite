using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler
{
    /// <summary>
    /// Base implementation of a <see cref="INodeWrangler"/> which handles the editing of <see cref="EbxAsset"/>
    /// </summary>
    public interface IEbxNodeWrangler : INodeWrangler
    {
        /// <summary>
        /// The asset being edited
        /// </summary>
        EbxAsset Asset { get; set; }

        /// <summary>
        /// Add a <see cref="IVertex"/> to the graph without editing the <see cref="Asset"/>
        /// </summary>
        /// <param name="vertex"></param>
        void AddVertexTransient(IVertex vertex);
        
        /// <summary>
        /// Add a <see cref="IConnection"/> to the graph without editing the <see cref="Asset"/>
        /// </summary>
        /// <param name="connection"></param>
        void AddConnectionTransient(IConnection connection);

        void ModifyAsset();
    }
}