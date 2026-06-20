using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Player;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefront.ValueNodes.Player
{
    public class WSPlayerFilterNode : PlayerFilterNode
    {
        public override string ObjectType => "WSPlayerFilterEntityData";
        public override string ToolTip => "Similar to PlayerFilterEntityData, this will output the player that caused this event to trigger as a player event";

        public override bool IsValid()
        {
            return ProfilesLibrary.ProfileName == "starwarsbattlefrontii" || ProfilesLibrary.ProfileName == "starwarsbattlefront";
        }
    }
}