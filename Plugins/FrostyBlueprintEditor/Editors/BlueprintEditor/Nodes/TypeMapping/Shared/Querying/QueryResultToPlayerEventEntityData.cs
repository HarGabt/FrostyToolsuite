using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Querying
{
	public class QueryResultToPlayerEventNode : EntityNode
	{
		public override string ObjectType => "QueryResultToPlayerEventEntityData";
        public override string ToolTip => "This node converts a Query Result into a Player Event";

        public override void OnCreation()
		{
			base.OnCreation();

			AddInput("Input", ConnectionType.Property, Realm);
			AddInput("Fire", ConnectionType.Event, Realm);

			AddOutput("Out", ConnectionType.Event, Realm);
		}
	}
}
