using FrostySdk.Managers;
using FrostySdk.Managers.Entries;

namespace BlueprintEditorPlugin.Editors.GraphEditor
{
    public interface IEbxGraphEditor : IGraphEditor
    {
        /// <summary>
        /// This method is used to determine if a GraphEditor is valid
        /// </summary>
        /// <returns>True if it is valid, false if it is not.</returns>
        bool IsValid();
        
        /// <summary>
        /// This method is used to determine if a GraphEditor is valid for editing an Ebx Asset
        /// </summary>
        /// <param name="assetEntry"></param>
        /// <returns>True if it is valid, false if it is not.</returns>
        bool IsValid(EbxAssetEntry assetEntry);
        
        void LoadAsset(EbxAssetEntry assetEntry);
    }
}