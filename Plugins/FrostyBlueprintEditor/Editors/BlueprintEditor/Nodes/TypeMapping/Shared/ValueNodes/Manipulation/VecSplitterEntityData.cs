using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Manipulation
{
    public class Vec2SplitterNode : EntityNode
    {
        public override string ObjectType => "Vec2SplitterEntityData";
        public override string ToolTip => "This node takes a Vec2 and outputs its floats";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("In", ConnectionType.Property, Realm);

            AddOutput("X", ConnectionType.Property, Realm);
            AddOutput("Y", ConnectionType.Property, Realm);
        }
    }
    
	public class Vec3SplitterNode : EntityNode
	{
		public override string ObjectType => "Vec3SplitterEntityData";
        public override string ToolTip => "This node takes a Vec3 and outputs its floats";

        public override void OnCreation()
		{
			base.OnCreation();

			AddInput("In", ConnectionType.Property, Realm);

			AddOutput("X", ConnectionType.Property, Realm);
			AddOutput("Y", ConnectionType.Property, Realm);
			AddOutput("Z", ConnectionType.Property, Realm);
		}
	}
    
    public class Vec4SplitterNode : EntityNode
    {
        public override string ObjectType => "Vec4SplitterEntityData";
        public override string ToolTip => "This node takes a Vec4 and outputs its floats";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("In", ConnectionType.Property, Realm);

            AddOutput("X", ConnectionType.Property, Realm);
            AddOutput("Y", ConnectionType.Property, Realm);
            AddOutput("Z", ConnectionType.Property, Realm);
            AddOutput("W", ConnectionType.Property, Realm);
        }
    }
}
