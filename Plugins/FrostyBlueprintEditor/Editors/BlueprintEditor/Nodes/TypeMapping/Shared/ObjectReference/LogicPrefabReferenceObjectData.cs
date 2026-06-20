namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ObjectReference
{
    public class LogicPrefabReferenceNode : BaseLogicRefEntity
    {
        public override string ObjectType => "LogicPrefabReferenceObjectData";
        public override string ToolTip => "This node allows you to interface with a Logic Prefab, allowing you to trigger and be triggered by it's internal logic";
    }
}