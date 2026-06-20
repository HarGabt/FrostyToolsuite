using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefront.ObjectReference
{
	public class SoldierBlueprintSpawnNode : EntityNode
	{
		public override string ObjectType => "SoldierBlueprintSpawnEntityData";
        public override bool IsValid()
        {
            return ProfilesLibrary.ProfileName == "starwarsbattlefront";
        }

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("NewTransform", ConnectionType.Property, Realm);
			AddInput("SpawnPlayer", ConnectionType.Event, Realm);

			AddOutput("DynamicBlueprint", ConnectionType.Link, Realm);
			AddOutput("OnPlayerSpawned", ConnectionType.Event, Realm);
		}
	}
}
