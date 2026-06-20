using System.Collections.Generic;
using System.Linq;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;
using FrostySdk;
using FrostySdk.Ebx;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic.Converters
{
    public class EventToIntNode : EntityNode
    {
        public override string ObjectType => "EventToIntEntityData";
        public override string ToolTip => "Outputs an integer property depending on the last fired event";

        public override void OnCreation()
        {
            base.OnCreation();

            dynamic selections = TryGetProperty("Selections");
            foreach (dynamic selection in selections)
            {
                string name = Utils.GetString((int)selection.EventHash);
                AddInput(name, ConnectionType.Event, Realm);
            }

            AddOutput("Output", ConnectionType.Property, Realm);
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);

            // An event was edited
            if (args.Item.Parent.Name == "Selections")
            {
                EntityInput input = GetInput((int)((dynamic)args.OldValue).EventHash, ConnectionType.Event);
                input.Name = Utils.GetString((int)((dynamic)args.NewValue).EventHash);
                RefreshCache();
            }
            // The list itself was edited
            else if (args.Item.Name == "Selections")
            {
                switch (args.ModifiedArgs.Type)
                {
                    case ItemModifiedTypes.Insert:
                    case ItemModifiedTypes.Add:
                    {
                        AddInput(Utils.GetString((int)((dynamic)args.NewValue).EventHash), ConnectionType.Event);
                    } break;
                    case ItemModifiedTypes.Remove:
                    {
                        EntityInput input = GetInput((int)((dynamic)args.OldValue).EventHash, ConnectionType.Event);
                        RemoveInput(input);
                    } break;
                    case ItemModifiedTypes.Clear:
                    {
                        ClearInputs();
                    } break;
                    case ItemModifiedTypes.Assign:
                    {
                        Inputs.Clear();

                        dynamic selections = TryGetProperty("Selections");
                        foreach (dynamic selection in selections)
                        {
                            string name = Utils.GetString((int)selection.EventHash);
                            AddInput(name, ConnectionType.Event, Realm);
                        }
                        
                        RefreshCache();
                    } break;
                }
            }
        }
    }
}