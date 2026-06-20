using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefront.ForceCard
{
	public class ForceCardSlotNode : EntityNode
	{
		public override string ObjectType => "ForceCardSlotEntityData";
        public override bool IsValid()
        {
            return ProfilesLibrary.ProfileName == "starwarsbattlefront";
        }

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("Query", ConnectionType.Event, Realm);

			AddOutput("IsEquipped", ConnectionType.Property, Realm);
		}
	}
}
