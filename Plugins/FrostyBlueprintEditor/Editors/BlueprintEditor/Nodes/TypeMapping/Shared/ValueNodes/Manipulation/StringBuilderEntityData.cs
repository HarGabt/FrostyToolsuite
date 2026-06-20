using System.Collections.Generic;
using System.Linq;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;
using FrostySdk;
using FrostySdk.Ebx;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Manipulation
{
    public class StringBuilderEntityData : EntityNode
    {
        public override string ObjectType => "StringBuilderEntityData";
        public override string ToolTip => "This lets you create a string from constants in the node and inputs";

        public override void OnCreation()
        {
            base.OnCreation();

            AddOutput("Output", ConnectionType.Property, Realm);
            AddInput("Sid", ConnectionType.Property, Realm);

            dynamic entries = TryGetProperty("Entries");
            if (entries == null)
                return;
            
            foreach (dynamic entry in entries)
            {
                string type = entry.TextType.ToString();
                if (type == "StringBuilderTextEntryType_Passthrough")
                    continue;

                string inputName = Utils.GetString(entry.FieldHash);
                AddInput(inputName, ConnectionType.Property, Realm);
            }
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);

            if (args.Item.Parent.Parent != null)
            {
                switch (args.Item.Name)
                {
                    case "TextType":
                    {
                        string type = args.NewValue.ToString();
                        if (type == "StringBuilderTextEntryType_Passthrough")
                            return;

                        dynamic entry = args.Item.Parent.Value;
                        int hash = (int)entry.FieldHash;
                        EntityInput input = GetInput(hash, ConnectionType.Property);
                        if (input == null)
                        {
                            input = AddInput(Utils.GetString(hash), ConnectionType.Property, Realm);
                        }
                        else
                        {
                            input.Name = Utils.GetString(hash);
                        }
                    } break;
                    case "FieldHash":
                    {
                        EntityInput input = GetInput(Utils.GetString((int)args.OldValue), ConnectionType.Property);
                        if (input == null)
                        {
                            AddInput(Utils.GetString((int)args.NewValue), ConnectionType.Property, Realm);
                            return;
                        }
                        
                        input.Name = Utils.GetString((int)args.NewValue);
                    } break;
                }
                RefreshCache();
            }
            // The list itself was edited
            else if (args.Item.Name == "Entries")
            {
                switch (args.ModifiedArgs.Type)
                {
                    case ItemModifiedTypes.Insert:
                    case ItemModifiedTypes.Add:
                    {
                        dynamic entry = args.NewValue;
                        string type = entry.TextType.ToString();
                        if (type == "StringBuilderTextEntryType_Passthrough")
                            return;

                        string name = Utils.GetString(entry.FieldHash);
                        AddInput(name, ConnectionType.Property, Realm);
                    } break;
                    case ItemModifiedTypes.Remove:
                    {
                        int hash = (int)((dynamic)args.OldValue).FieldHash;
                        if (hash == 0)
                            return;
                        
                        EntityInput input = GetInput(hash, ConnectionType.Property);
                        RemoveInput(input);
                    } break;
                    case ItemModifiedTypes.Clear:
                    {
                        List<IPort> inputs = Inputs.ToList();

                        for (var i = 1; i < inputs.Count; i++)
                        {
                            IPort input = inputs[i];
                            RemoveInput((EntityInput)input);
                        }
                    } break;
                    case ItemModifiedTypes.Assign:
                    {
                        Inputs.Clear();
                        Outputs.Clear();
                        
                        dynamic entries = TryGetProperty("Entries");
                        if (entries == null)
                            return;
                        
                        foreach (dynamic entry in entries)
                        {
                            string type = entry.TextType.ToString();
                            if (type == "StringBuilderTextEntryType_Passthrough")
                                continue;

                            string inputName = Utils.GetString(entry.FieldHash);
                            AddInput(inputName, ConnectionType.Property, Realm);
                        }
                        
                        RefreshCache();
                    } break;
                }
            }
        }

        public override void BuildFooter()
        {
            ClearFooter();
            
            string footer = "";
            string sid = TryGetProperty("Sid").ToString();
            if (!string.IsNullOrEmpty(sid))
            {
                AddFooter(sid); // TODO: We should be filling in the args in the string
                return;
            }

            dynamic entries = TryGetProperty("Entries");
            if (entries == null)
                return;

            foreach (dynamic entry in entries)
            {
                string type = entry.TextType.ToString();
                if (type == "StringBuilderTextEntryType_Passthrough")
                {
                    footer += entry.Text.ToString();
                }
                else
                {
                    footer += "{" + $"{Utils.GetString(entry.FieldHash)}" + "}";
                }
            }
            
            if (footer == "")
                return;

            AddFooter(footer);
        }
    }
}