using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic.Timing
{
    public class StopWatchNode : EntityNode
    {
        public override string ObjectType => "StopWatchEntityData";

        public override string ToolTip => "Keeps track of elapsed time, useful for when keeping track of how long it's been since an event fired";

        public override void OnCreation()
        {
            base.OnCreation();

            if (TryGetProperty("TriggerOnTime") != null && (float)TryGetProperty("TriggerOnTime") != 0)
            {
                AddFooter($"Triggers at: {TryGetProperty("TriggerOnTime")}");
            }
            
            if (TryGetProperty("Multiplier") != null && (float)TryGetProperty("Multiplier") != 0)
            {
                AddFooter($"Speed Multiplier: {TryGetProperty("Multiplier")}");
            }
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);
            
            AddInput("TriggerOnTime", ConnectionType.Property);
            AddInput("Multiplier", ConnectionType.Property);
            
            AddInput("Start", ConnectionType.Event);
            AddInput("Stop", ConnectionType.Event);
            AddInput("Reset", ConnectionType.Event);

            AddOutput("Time", ConnectionType.Event);
            AddOutput("OnTrigger", ConnectionType.Event);

            ClearFooter();
            switch (args.Item.Name)
            {
                case "TriggerOnTime":
                {
                    if (TryGetProperty("TriggerOnTime") != null && (int)TryGetProperty("TriggerOnTime") != 0)
                    {
                        AddFooter($"Triggers at: {TryGetProperty("TriggerOnTime")}");
                    }
                } break;
                case "Multiplier":
                {
                    if (TryGetProperty("Multiplier") != null && (int)TryGetProperty("Multiplier") != 0)
                    {
                        AddFooter($"Speed Multiplier: {TryGetProperty("Multiplier")}");
                    }
                } break;
            }
        }
    }
}