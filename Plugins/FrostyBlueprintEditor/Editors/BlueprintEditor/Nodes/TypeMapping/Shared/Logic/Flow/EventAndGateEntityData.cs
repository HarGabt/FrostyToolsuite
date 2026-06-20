using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic.Flow
{
	public class EventAndGateNode : EntityNode
	{
		public override string ObjectType => "EventAndGateEntityData";
        public override string ToolTip => "This node only fires an event when all In events have triggered";

        public override void OnCreation()
		{
			base.OnCreation();

            AddInput("Reset", ConnectionType.Event, Realm);
            if (TryGetProperty("EventCount") != null)
            {
                uint eventCount = (uint)TryGetProperty("EventCount");
                for (int i = 0; i < eventCount; i++)
                {
                    AddInput($"In{i + 1}", ConnectionType.Event, Realm);
                }
            }

            AddOutput("Out", ConnectionType.Event, Realm);
		}

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);

            if (args.Item.Name == "EventCount")
            {
                uint oldCount = (uint)args.OldValue;
                uint inCount = (uint)TryGetProperty("EventCount");

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
                        if (GetInput($"In{i + 1}", ConnectionType.Event) != null)
                            continue;
                        
                        AddInput($"In{i + 1}", ConnectionType.Event, Realm);
                    }
                }
                else
                {
                    for (uint i = oldCount; i > 1; i--)
                    {
                        RemoveInput($"In{i + 1}", ConnectionType.Event);
                    }
                }
            }
        }
    }
}
