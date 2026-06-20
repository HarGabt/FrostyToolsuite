using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefrontII.AutoPlayers
{
    public class AutoPlayerInteractObjectiveNode : AutoPlayerObjectiveNode
    {
        public override string ObjectType => "AutoPlayerInteractObjectiveEntityData";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("InteractPosition", ConnectionType.Property, Realm);
            AddInput("InteractEntityPosition", ConnectionType.Property, Realm);

            AddOutput("OnInteracted", ConnectionType.Event, Realm);
        }
    }
}