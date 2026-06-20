using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefrontII.AutoPlayers
{
    public class AutoPlayerMoveObjectiveNode : AutoPlayerObjectiveNode
    {
        public override string ObjectType => "AutoPlayerMoveObjectiveEntityData";

        public override void OnCreation()
        {
            base.OnCreation();
            AddInput("TargetPosition", ConnectionType.Property, Realm);
            AddOutput("OnTargetReached", ConnectionType.Event, Realm, true);
            AddOutput("0x92f8a646", ConnectionType.Property, Realm, true);
        }
    }
}