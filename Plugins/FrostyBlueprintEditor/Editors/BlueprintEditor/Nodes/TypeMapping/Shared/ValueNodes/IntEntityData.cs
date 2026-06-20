namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes
{
    public class IntNode : BaseNumberNode
    {
        public override string ObjectType => "IntEntityData";

        public override string ToolTip => "This node stores an integer, a whole number, with an adjustable value";
    }
    
    public class UIntNode : BaseNumberNode
    {
        public override string ObjectType => "UIntEntityData";

        public override string ToolTip => "This node stores an unsigned integer, a whole number, with an adjustable value";
    }
}