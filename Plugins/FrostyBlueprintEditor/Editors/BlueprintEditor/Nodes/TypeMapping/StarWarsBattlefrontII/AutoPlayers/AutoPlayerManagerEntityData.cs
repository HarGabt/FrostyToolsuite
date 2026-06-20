using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefrontII.AutoPlayers
{
    public class AutoPlayerManagerNode : EntityNode
    {
        public override string ObjectType => "AutoPlayerManagerEntityData";
        
        public override bool IsValid()
        {
            return ProfilesLibrary.ProfileName == "starwarsbattlefrontii" || ProfilesLibrary.ProfileName == "bfv";
        }

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("PlayerCount", ConnectionType.Property, Realm.Server);
            AddInput("FillGameplayBotsTeam1", ConnectionType.Property, Realm.Server);
            AddInput("FillGameplayBotsTeam2", ConnectionType.Property, Realm.Server);
            AddInput("TriggerOrphans", ConnectionType.Event, Realm.Server);
            AddInput("0x18156c34", ConnectionType.Event, Realm.Server); // TODO: Solve this hash?

            AddOutput("Orphan", ConnectionType.Event, Realm.Server, true);
        }
    }
}