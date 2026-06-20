namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ObjectReference
{
    public class SpatialPrefabReferenceNode : BaseLogicRefEntity
    {
        public override string ObjectType => "SpatialPrefabReferenceObjectData";
        public override string ToolTip => "This node creates a Spatial Prefab which can be accessed and placed in the world";
    }
}