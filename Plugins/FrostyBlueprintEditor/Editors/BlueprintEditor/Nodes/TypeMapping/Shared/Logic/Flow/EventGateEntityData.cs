using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic.Flow
{
    public class EventGateNode : EntityNode
    {
        public override string ObjectType => "EventGateEntityData";

        public override void OnCreation()
        {
            base.OnCreation();
            
            AddInput("In", ConnectionType.Event);
            AddInput("Open", ConnectionType.Event);
            AddInput("Close", ConnectionType.Event);

            AddOutput("Out", ConnectionType.Event);
        }

        public override void BuildFooter()
        {
            bool def = false;
            if (TryGetProperty("DefaultOpen") != null)
            {
                def = (bool)TryGetProperty("DefaultOpen");
            }
            else
            {
                if (TryGetProperty("Default") == null)
                    return;
                
                def = (bool)TryGetProperty("Default");
            }
            
            if (def)
            {
                AddFooter("Default: Open");
            }
            else
            {
                RemoveFooter("Default: Closed");
            }
        }
    }
}