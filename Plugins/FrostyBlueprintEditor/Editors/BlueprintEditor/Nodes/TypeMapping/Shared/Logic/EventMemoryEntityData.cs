using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic
{
    public class EventMemoryNode : EntityNode
    {
        public override string ObjectType => "EventMemoryEntityData";
        public override string ToolTip => "This node \"Memorizes\" an event occuring.";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("LoadMemory", ConnectionType.Event, Realm);
            AddInput("FireMemory", ConnectionType.Event, Realm);
            AddInput("ClearMemory", ConnectionType.Event, Realm);

            AddOutput("OnMemoryEvent", ConnectionType.Event, Realm);
        }
    }
}