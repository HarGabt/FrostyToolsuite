using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefront.ValueNodes.Player
{
	public class ToPartnerEventNode : EntityNode
	{
		public override string ObjectType => "ToPartnerEventEntityData";
        public override bool IsValid()
        {
            return ProfilesLibrary.ProfileName == "starwarsbattlefront";
        }

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("In", ConnectionType.Event, Realm);

			AddOutput("Out", ConnectionType.Event, Realm);
			AddOutput("0x92b9b70b", ConnectionType.Event, Realm);
		}
	}
}
