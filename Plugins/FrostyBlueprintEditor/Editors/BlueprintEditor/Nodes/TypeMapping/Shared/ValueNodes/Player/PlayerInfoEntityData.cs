using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Player
{
    public class PlayerInfoNode : EntityNode
    {
        public override string ObjectType => "PlayerInfoEntityData";
        public override string ToolTip => "This node allows you to get info on a player from a Player Event";

        public override void OnCreation()
        {
            base.OnCreation();
            
            AddInput("SetPlayer", ConnectionType.Event, Realm);
            AddOutput("Name", ConnectionType.Property, Realm);
            AddOutput("Team", ConnectionType.Property, Realm);
        }
    }
}