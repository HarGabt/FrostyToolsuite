namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ObjectReference
{
    public class CharacterSpawnNode : BaseLogicRefEntity
    {
        public override string ObjectType => "CharacterSpawnReferenceObjectData";

        public override string ToolTip => "This node spawns a character in using the specified Soldier Blueprint";

        public override void OnCreation()
        {
            base.OnCreation();
        }
    }
}