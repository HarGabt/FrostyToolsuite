using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic.Flow
{
    public class EventSplitterNode : EntityNode
    {
        public override string ObjectType => "EventSplitterEntityData";
        public override string ToolTip => "This node is mostly used for organization, though events can be set to RunOnce.\nPretty useless if you ask me, would rather have reroutes...";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("Impulse", ConnectionType.Event, Realm);
            AddOutput("OnImpulse", ConnectionType.Event, Realm);
        }
    }
}