using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefront.Customization
{
	public class AppearanceCustomizationNode : EntityNode
	{
		public override string ObjectType => "AppearanceCustomizationEntityData";

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("0xd501f006", ConnectionType.Event, Realm);
			AddInput("0xc1faf619", ConnectionType.Event, Realm);
			AddInput("0x2f2ebfa7", ConnectionType.Event, Realm);
			AddInput("HeadIndex", ConnectionType.Property, Realm);
			AddInput("BodyIndex", ConnectionType.Property, Realm);

		}
	}
}
