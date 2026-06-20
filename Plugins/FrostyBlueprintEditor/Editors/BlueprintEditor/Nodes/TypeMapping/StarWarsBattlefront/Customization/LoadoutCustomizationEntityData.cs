using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefront.Customization
{
	public class LoadoutCustomizationNode : EntityNode
	{
		public override string ObjectType => "LoadoutCustomizationEntityData";
        public override bool IsValid()
        {
            return ProfilesLibrary.ProfileName == "starwarsbattlefront";
        }

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("WeaponId", ConnectionType.Property, Realm);
			AddInput("0xe3da9419", ConnectionType.Event, Realm);

			AddOutput("WeaponId", ConnectionType.Property, Realm);
		}
	}
}
