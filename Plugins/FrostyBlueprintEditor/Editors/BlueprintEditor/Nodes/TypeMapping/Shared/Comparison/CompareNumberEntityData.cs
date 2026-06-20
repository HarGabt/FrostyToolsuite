using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Comparison
{
	public abstract class CompareNumberNode : EntityNode
	{
		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("A", ConnectionType.Property, Realm);
			AddInput("B", ConnectionType.Property, Realm);
			AddInput("In", ConnectionType.Event, Realm);

			AddOutput("A=B", ConnectionType.Event, Realm);
			AddOutput("A!=B", ConnectionType.Event, Realm);
			AddOutput("A>=B", ConnectionType.Event, Realm);
			AddOutput("A<=B", ConnectionType.Event, Realm);
			
			AddOutput("A>B", ConnectionType.Event, Realm);
			AddOutput("A<B", ConnectionType.Event, Realm);
		}
	}

	public class CompareIntNode : CompareNumberNode
	{
		public override string ObjectType => "CompareIntEntityData";
	}
	
	public class CompareFloatNode : CompareNumberNode
	{
		public override string ObjectType => "CompareFloatEntityData";
	}
}
