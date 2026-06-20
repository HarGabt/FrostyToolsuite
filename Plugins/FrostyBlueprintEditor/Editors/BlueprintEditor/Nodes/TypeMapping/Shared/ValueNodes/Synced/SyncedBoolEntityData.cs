using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Synced
{
    public class SyncedBoolNode : SyncedNode
    {
        public override string ObjectType => "SyncedBoolEntityData";
        public override string ToolTip => "This node syncs a boolean value between all clients connected to the server.";

        public override void OnCreation()
        {
            base.OnCreation();
            
            AddOutput("OnTrue", ConnectionType.Event, Realm.Server);
            AddOutput("OnFalse", ConnectionType.Event, Realm.Server);

            AddInput("SetTrue", ConnectionType.Event, Realm.Server);
            AddInput("SetFalse", ConnectionType.Event, Realm.Server);
            AddInput("Toggle", ConnectionType.Event, Realm.Server);
        }
    }
}
