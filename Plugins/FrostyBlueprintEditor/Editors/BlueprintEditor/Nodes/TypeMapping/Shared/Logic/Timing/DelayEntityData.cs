using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic.Timing
{
    public class DelayEntityData : EntityNode
    {
        public override string ObjectType => "DelayEntityData";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("Delay", ConnectionType.Property, Realm);
            AddInput("In", ConnectionType.Event, Realm);
            AddInput("Reset", ConnectionType.Event, Realm);
            AddOutput("Out", ConnectionType.Event, Realm);
        }
        
        public override void BuildFooter()
        {
            AddFooter($"Delay: {TryGetProperty("Delay")}");
            if ((bool)TryGetProperty("AutoStart"))
            {
                AddFooter("Auto starts");
            }
            
            if ((bool)TryGetProperty("RunOnce"))
            {
                AddFooter("Runs once");
            }
        }
    }
}