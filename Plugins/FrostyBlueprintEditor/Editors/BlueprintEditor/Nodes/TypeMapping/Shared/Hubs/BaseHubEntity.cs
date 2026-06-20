using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Hubs
{
    /// <summary>
    /// Base implementation for a hub
    /// </summary>
    public abstract class BaseHubEntity : EntityNode
    {
        public override string ToolTip => "This node outputs the input at the SelectedIndex.";

        public override void OnCreation()
        {
            base.OnCreation();

            dynamic hashedInputs = TryGetProperty("HashedInput");
            foreach (UInt32 hashedInput in hashedInputs)
            {
                string unHash = Utils.GetString((int)hashedInput);

                AddInput(unHash, ConnectionType.Property, Realm);
            }

            if (TryGetProperty("InputCount") != null)
            {
                Footer = $"Has {TryGetProperty("InputCount")} Usable inputs";
            }
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);
            
            // A hash was edited
            if (args.Item.Parent.Name == "HashedInput")
            {
                uint old = (uint)args.OldValue;
                uint newV = (uint)args.NewValue;
                
                EntityInput input = GetInput((int)old, ConnectionType.Property);
                input.Name = Utils.GetString((int)newV);
            }
            else switch (args.Item.Name)
            {
                // The list itself was edited
                case "HashedInput":
                {
                    switch (args.ModifiedArgs.Type)
                    {
                        case ItemModifiedTypes.Insert:
                        case ItemModifiedTypes.Add:
                        {
                            uint newV = (uint)args.NewValue;
                            AddInput(Utils.GetString((int)newV), ConnectionType.Property, Realm);
                        } break;
                        case ItemModifiedTypes.Remove:
                        {
                            int index = Convert.ToInt32(args.OldValue);
                            EntityInput input = GetInput(index, ConnectionType.Property);
                            if (input == null)
                                {
                                    return;
                                }
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
                    }
                } break;
                case "InputCount":
                {
                    Footer = $"Has {TryGetProperty("InputCount")} Usable inputs";
                } break;
            }
        }

        public BaseHubEntity()
        {
            Inputs = new ObservableCollection<IPort>()
            {
                new PropertyInput("InputSelect", this)
            };

            Outputs = new ObservableCollection<IPort>
            {
                new PropertyOutput("Out", this)
            };
        }
    }
}
