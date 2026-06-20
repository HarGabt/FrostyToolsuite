using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using Frosty.Core;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Extensions
{
    public class SelectAssetMenuExtension : BlueprintMenuItemExtension
    {
        public override string DisplayName => "Select Opened Asset";
        public override string ToolTip => "Selects the asset this is a graph of in the Data Explorer";

        public override RelayCommand ButtonClicked => new RelayCommand(o =>
        {
            if (GraphEditor.NodeWrangler is EntityNodeWrangler wrangler)
            {
                App.EditorWindow.DataExplorer.SelectAsset(wrangler.AssetEntry);
            }
        });
    }
}