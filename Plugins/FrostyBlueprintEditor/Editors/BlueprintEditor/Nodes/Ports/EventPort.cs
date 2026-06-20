using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports
{
    public class EventInput : EntityInput
    {
        public override PortDirection Direction => PortDirection.In;
        public override ConnectionType Type => ConnectionType.Event;

        public EventInput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }

    public class EventOutput : EntityOutput
    {
        public override PortDirection Direction => PortDirection.Out;
        public override ConnectionType Type => ConnectionType.Event;

        public EventOutput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }
}