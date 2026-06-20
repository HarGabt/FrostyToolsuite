using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Player
{
	public class PlayerEventCacheNode : EntityNode
	{
		public override string ObjectType => "PlayerEventCacheEntityData";

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("0xf517333a", ConnectionType.Event, Realm);

			AddOutput("Player", ConnectionType.Property, Realm);
			AddOutput("OnPlayerEvent", ConnectionType.Event, Realm);
		}
	}
}
