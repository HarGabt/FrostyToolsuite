using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefront.ObjectReference
{
	public class BlueprintBundleNode : EntityNode
	{
		public override string ObjectType => "BlueprintBundleEntityData";
        public override bool IsValid()
        {
            return ProfilesLibrary.ProfileName == "starwarsbattlefront";
        }

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("Blueprint", ConnectionType.Link, Realm);
			AddInput("StreamOut", ConnectionType.Event, Realm);
			AddInput("StreamIn", ConnectionType.Event, Realm);

			AddOutput("OnStreamedIn", ConnectionType.Event, Realm);
		}
	}
}
