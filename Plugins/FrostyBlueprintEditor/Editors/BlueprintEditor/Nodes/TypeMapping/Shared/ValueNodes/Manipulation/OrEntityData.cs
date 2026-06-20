using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Manipulation
{
    public class OrNode : EntityNode
    {
        public override string ObjectType => "OrEntityData";
        public override string ToolTip => "This node outputs true whenever any inputs are true";

        public override void OnCreation()
        {
            base.OnCreation();

            uint inCount = (uint)TryGetProperty("InputCount");

            for (int i = 1; i <= inCount; i++)
            {
                AddInput($"In{i}", ConnectionType.Property, Realm);
            }

            AddOutput("Out", ConnectionType.Property, Realm);
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);

            if (args.Item.Name == "InputCount")
            {
                uint oldCount = (uint)args.OldValue;
                uint inCount = (uint)TryGetProperty("InputCount");

                if (inCount == 0)
                {
                    ClearInputs();
                    return;
                }
                
                if (oldCount < inCount)
                {
                    // Add new inputs
                    for (uint i = 1; i <= inCount; i++)
                    {
                        if (GetInput($"In{i}", ConnectionType.Property) != null)
                            continue;
                        
                        AddInput($"In{i}", ConnectionType.Property, Realm);
                    }
                }
                else
                {
                    for (uint i = oldCount; i > 1; i--)
                    {
                        RemoveInput($"In{i}", ConnectionType.Property);
                    }
                }
            }
        }
    }
}