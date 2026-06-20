using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefront.ForceCard
{
	public class ForceCardPickupNode : EntityNode
	{
		public override string ObjectType => "ForceCardPickupEntityData";
        public override bool IsValid()
        {
            return ProfilesLibrary.ProfileName == "starwarsbattlefront";
        }

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("In", ConnectionType.Event, Realm);

		}
	}
}
