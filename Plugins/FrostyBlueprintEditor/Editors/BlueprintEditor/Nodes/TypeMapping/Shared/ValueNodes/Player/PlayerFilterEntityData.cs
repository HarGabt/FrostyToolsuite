using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Player
{
    public class PlayerFilterNode : EntityNode
    {
        public override string ObjectType => "PlayerFilterEntityData";

        public override string ToolTip => "This will output the player that caused this event to trigger as a player event";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("In", ConnectionType.Event, Realm);
            AddOutput("Controllable", ConnectionType.Link, Realm);
            AddOutput("OnTriggerOnlyForControllable", ConnectionType.Event, Realm, true);
            AddOutput("OnTriggerOnlyForPlayer", ConnectionType.Event, Realm, true);
            AddOutput("OnTriggeredByHost", ConnectionType.Event, Realm, true);
            AddOutput("OnTriggerOnlyForHost", ConnectionType.Event, Realm, true);
        }
    }
}