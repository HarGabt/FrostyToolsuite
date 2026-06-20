using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic.Flow
{
	public class PropertyGateNode : EntityNode
	{
		public override string ObjectType => "PropertyGateEntityData";

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("Open", ConnectionType.Event, Realm);
			AddInput("Close", ConnectionType.Event, Realm);
			AddInput("BoolIn", ConnectionType.Property, Realm);
			AddInput("UintIn", ConnectionType.Property, Realm);
			AddInput("IntIn", ConnectionType.Property, Realm);
			AddInput("StringIn", ConnectionType.Property, Realm);
			AddInput("FloatIn", ConnectionType.Property, Realm);
			AddInput("Vec3In", ConnectionType.Property, Realm);
			AddInput("Default", ConnectionType.Property, Realm);
			AddInput("WritePropertyOnOpenGate", ConnectionType.Property, Realm);

			AddOutput("BoolOut", ConnectionType.Property, Realm);
			AddOutput("UintOut", ConnectionType.Property, Realm);
			AddOutput("IntOut", ConnectionType.Property, Realm);
			AddOutput("StringOut", ConnectionType.Property, Realm);
			AddOutput("FloatOut", ConnectionType.Property, Realm);
			AddOutput("Vec3Out", ConnectionType.Property, Realm);
		}
	}
}
