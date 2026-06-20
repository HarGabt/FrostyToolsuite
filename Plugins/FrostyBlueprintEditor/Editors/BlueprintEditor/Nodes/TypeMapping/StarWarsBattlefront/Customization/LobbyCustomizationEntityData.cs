using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefront.Customization
{
	public class LobbyCustomizationNode : EntityNode
	{
		public override string ObjectType => "LobbyCustomizationEntityData";
        public override bool IsValid()
        {
            return ProfilesLibrary.ProfileName == "starwarsbattlefront";
        }

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("PrimaryDeckIndex", ConnectionType.Property, Realm);
			AddInput("0xe3da9419", ConnectionType.Event, Realm);

		}
	}
}
