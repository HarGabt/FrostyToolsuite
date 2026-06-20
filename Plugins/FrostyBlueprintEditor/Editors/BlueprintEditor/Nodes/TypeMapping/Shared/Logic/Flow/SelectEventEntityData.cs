using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;
using FrostySdk.Ebx;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic.Flow
{
    public class SelectEventNode : EntityNode
    {
        public override string ObjectType => "SelectEventEntityData";
        public override string ToolTip => "This node lets you to select between a list of output events";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("In", ConnectionType.Event, Realm);
            AddOutput("Selected", ConnectionType.Property, Realm);

            foreach (CString evnt in (dynamic)TryGetProperty("Events"))
            {
                if (evnt.IsNull())
                    continue;

                AddOutput(evnt.ToString(), ConnectionType.Event, Realm);
                AddInput($"Select{evnt.ToString()}", ConnectionType.Event);
            }
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);

            // An event was edited
            if (args.Item.Parent.Name == "Events")
            {
                string oldName = (args.OldValue == null || ((CString)args.OldValue).IsNull()) ? "Event" : args.OldValue.ToString();
                EntityInput input = GetInput($"Select{oldName}", ConnectionType.Event);
                EntityOutput output = GetOutput(oldName, ConnectionType.Event);
                // Update names to the new value
                string newName = args.NewValue.ToString();
                input.Name = newName;
                output.Name = $"Select{newName}";
                RefreshCache();
            }
            // The list itself was edited
            else if (args.Item.Name == "Events")
            {
                switch (args.ModifiedArgs.Type)
                {
                    case ItemModifiedTypes.Insert:
                    case ItemModifiedTypes.Add:
                    {
                        CString eventName = (dynamic)args.NewValue;
                        if (eventName.IsNull())
                            break;
                        AddOutput(args.NewValue.ToString(), ConnectionType.Event, Realm);
                        AddInput($"Select{args.NewValue.ToString()}", ConnectionType.Event, Realm);
                    } break;
                    case ItemModifiedTypes.Remove:
                    {
                        CString eventName = (dynamic)args.OldValue;
                        if (eventName.IsNull())
                            break;
                        
                        EntityOutput output = GetOutput(eventName.ToString(), ConnectionType.Event);
                        RemoveOutput(output);

                        EntityInput input = GetInput($"Select{eventName.ToString()}", ConnectionType.Event);
                        RemoveInput(input);
                    } break;
                    case ItemModifiedTypes.Clear:
                    {
                        List<IPort> inputs = Inputs.ToList();
                        List<IPort> outputs = Outputs.ToList();

                        for (var i = 1; i < inputs.Count; i++)
                        {
                            IPort input = inputs[i];
                            RemoveInput((EntityInput)input);
                        }
                        
                        for (var i = 1; i < outputs.Count; i++)
                        {
                            IPort output = outputs[i];
                            RemoveOutput((EntityOutput)output);
                        }
                    } break;
                    case ItemModifiedTypes.Assign:
                    {
                        Inputs.Clear();
                        Outputs.Clear();
                        
                        foreach (CString evnt in (dynamic)TryGetProperty("Events"))
                        {
                            if (evnt.IsNull())
                                continue;
                
                            AddOutput(evnt.ToString(), ConnectionType.Event, Realm);
                            AddInput($"Select{evnt.ToString()}", ConnectionType.Event, Realm);
                        }
                        
                        RefreshCache();
                    } break;
                }
            }
        }

        public SelectEventNode()
        {
            Inputs = new ObservableCollection<IPort>
            {
                new EventInput("In", this),
                new PropertyInput("Selected", this)
            };
        }
    }
}
