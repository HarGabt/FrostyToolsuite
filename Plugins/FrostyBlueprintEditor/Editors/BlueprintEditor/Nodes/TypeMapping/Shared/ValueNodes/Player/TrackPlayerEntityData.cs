using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Player
{
    public class TrackPlayerEntityData : EntityNode
    {
        public override string ObjectType => "TrackPlayerEntityData";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("TrackPlayer", ConnectionType.Event, Realm);
            AddInput("Reset", ConnectionType.Event, Realm);

            AddOutput("OnTrackedPlayerKilled", ConnectionType.Event, Realm, true);
            AddOutput("OnTrackedPlayerDestroy", ConnectionType.Event, Realm, true);
            AddOutput("0x3da82ed0", ConnectionType.Event, Realm, true);
        }
    }
}