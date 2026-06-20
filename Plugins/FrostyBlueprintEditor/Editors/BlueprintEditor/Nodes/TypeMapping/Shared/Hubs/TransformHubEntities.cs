namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Hubs
{
    public class Vec2HubNode : BaseHubEntity
    {
        public override string ObjectType => "Vec2HubEntityData";
    }
    
    public class Vec3HubNode : BaseHubEntity
    {
        public override string ObjectType => "Vec3HubEntityData";
    }
    
    public class TransformHubNode : BaseHubEntity
    {
        public override string ObjectType => "TransformHubEntityData";
    }
}