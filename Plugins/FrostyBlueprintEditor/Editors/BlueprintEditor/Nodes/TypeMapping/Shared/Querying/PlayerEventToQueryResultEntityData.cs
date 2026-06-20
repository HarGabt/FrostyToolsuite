using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Querying
{
	public class PlayerEventToQueryResultNode : EntityNode
	{
		public override string ObjectType => "PlayerEventToQueryResultEntityData";
        public override string ToolTip => "This node converts a Player Event into a Query Result";

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("0xf517333a", ConnectionType.Event, Realm);

			AddOutput("Output", ConnectionType.Property, Realm);
		}
	}
}
