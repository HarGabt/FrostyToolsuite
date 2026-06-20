using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Querying
{
	public class GeometryTriggerNode : EntityNode
	{
		public override string ObjectType => "GeometryTriggerEntityData";
        public override string ToolTip => "This node checks within an area or geometry for when something enters it.\nFor example a player entering inside a sphere";

        public override void OnCreation()
		{
			base.OnCreation();

            AddInput("Enable", ConnectionType.Event, Realm);
            AddInput("Disable", ConnectionType.Event, Realm);
			AddInput("Enabled", ConnectionType.Property, Realm);

			AddOutput("Geometry", ConnectionType.Link, Realm);
			AddOutput("OnEnter", ConnectionType.Event, Realm);
			AddOutput("OnLeave", ConnectionType.Event, Realm);
			AddOutput("OnInsideArea", ConnectionType.Event, Realm);
		}
	}
}
