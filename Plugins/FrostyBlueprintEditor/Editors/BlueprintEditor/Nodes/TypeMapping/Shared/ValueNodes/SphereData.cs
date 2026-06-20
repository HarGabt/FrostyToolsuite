using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes
{
	public class SphereDataNode : EntityNode
	{
		public override string ObjectType => "SphereData";

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("0x00000000", ConnectionType.Link, Realm);

		}
	}
}
