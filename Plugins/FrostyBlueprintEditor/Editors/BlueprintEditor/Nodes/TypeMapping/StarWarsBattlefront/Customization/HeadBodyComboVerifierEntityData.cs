using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefront.Customization
{
	public class HeadBodyComboVerifierNode : EntityNode
	{
		public override string ObjectType => "HeadBodyComboVerifierEntityData";

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("WantedHeadIndex", ConnectionType.Property, Realm);
			AddInput("WantedBodyIndex", ConnectionType.Property, Realm);
			AddInput("TeamId", ConnectionType.Property, Realm);

			AddOutput("0x69b0190e", ConnectionType.Property, Realm);
			AddOutput("BodyIdentifier", ConnectionType.Property, Realm);
			AddOutput("0xea17cea0", ConnectionType.Property, Realm);
			AddOutput("HeadReplacement", ConnectionType.Property, Realm);
			AddOutput("HeadIndex", ConnectionType.Property, Realm);
			AddOutput("BodyIndex", ConnectionType.Property, Realm);
		}
	}
}
