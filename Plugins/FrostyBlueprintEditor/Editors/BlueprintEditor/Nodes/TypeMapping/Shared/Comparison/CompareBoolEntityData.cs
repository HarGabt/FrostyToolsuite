using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Comparison
{
    public class CompareBoolNode : EntityNode
    {
        public override string ObjectType => "CompareBoolEntityData";
        public override string ToolTip => "This node sends an output event depending on if the Input property is true";

        public override void BuildFooter()
        {
            ClearFooter();
            if ((bool)TryGetProperty("TriggerOnStart"))
            {
                Footer = "Triggers on start";
            }

            if ((bool)TryGetProperty("TriggerOnPropertyChange"))
            {
                if (Footer != null)
                {
                    Footer += ", ";
                }
                else
                {
                    Footer = "Triggers ";
                }

                Footer += "On property changed";
            }
            
            if (TryGetProperty("AlwaysSend") != null && (bool)TryGetProperty("AlwaysSend"))
            {
                if (Footer != null)
                {
                    Footer += "\n";
                }

                Footer += "Always sends outputs";
            }
            
            if (TryGetProperty("AlwaysSendOnEvent") != null && (bool)TryGetProperty("AlwaysSendOnEvent"))
            {
                if (Footer != null)
                {
                    Footer += "\n";
                }

                Footer += "Always sends when In is triggered";
            }
        }

        public CompareBoolNode()
        {
            Inputs = new ObservableCollection<IPort>()
            {
                new PropertyInput("Bool", this),
                new EventInput("In", this)
            };
            Outputs = new ObservableCollection<IPort>()
            {
                new EventOutput("OnTrue", this),
                new EventOutput("OnFalse", this)
            };
        }
    }
}