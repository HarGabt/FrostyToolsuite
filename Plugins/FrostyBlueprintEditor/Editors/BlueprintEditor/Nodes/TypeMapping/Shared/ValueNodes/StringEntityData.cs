using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes
{
    public class StringEntityData : EntityNode
    {
        public override string ObjectType => "StringEntityData";

        public override void BuildFooter()
        {
            Footer = $"Default string: {TryGetProperty("DefaultString")}";
        }
    }
}