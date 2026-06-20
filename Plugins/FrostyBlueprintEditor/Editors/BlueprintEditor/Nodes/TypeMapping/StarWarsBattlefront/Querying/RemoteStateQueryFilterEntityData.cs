using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefront.Querying
{
	public class RemoteStateQueryFilterNode : EntityNode
	{
		public override string ObjectType => "RemoteStateQueryFilterEntityData";
        public override bool IsValid()
        {
            return ProfilesLibrary.ProfileName == "starwarsbattlefront";
        }

        public override void OnCreation()
		{
			base.OnCreation();

			AddInput("EntitiesToFilter", ConnectionType.Property, Realm);
			AddInput("0x3dae2678", ConnectionType.Property, Realm);

			AddOutput("QueryReturnedOneOrMore", ConnectionType.Event, Realm);
            AddOutput("0x126e3e73", ConnectionType.Property, Realm);
        }
	}
}
