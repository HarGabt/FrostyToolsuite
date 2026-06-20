using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic
{
    public class EventSyncNode : EntityNode
    {
        public override string ObjectType => "EventSyncEntityData";

        public override void OnCreation()
        {
            base.OnCreation();
            AddInput("Client", ConnectionType.Event, Realm.Client);
            AddOutput("Out", ConnectionType.Event, Realm.Server);
        }
    }
}