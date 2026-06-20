using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports
{
    public class LinkInput : EntityInput
    {
        public override PortDirection Direction => PortDirection.In;
        public override ConnectionType Type => ConnectionType.Link;
        
        public LinkInput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }

    public class LinkOutput : EntityOutput
    {
        public override PortDirection Direction => PortDirection.Out;
        public override ConnectionType Type => ConnectionType.Link;
        
        public LinkOutput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }
}