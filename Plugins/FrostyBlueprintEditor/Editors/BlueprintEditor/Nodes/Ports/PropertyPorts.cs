using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports
{
    public class PropertyInput : EntityInput
    {
        public override PortDirection Direction => PortDirection.In;
        public override ConnectionType Type => ConnectionType.Property;

        public PropertyInput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }

    public class PropertyOutput : EntityOutput
    {
        public override PortDirection Direction => PortDirection.Out;
        public override ConnectionType Type => ConnectionType.Property;
        
        public PropertyOutput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }
}