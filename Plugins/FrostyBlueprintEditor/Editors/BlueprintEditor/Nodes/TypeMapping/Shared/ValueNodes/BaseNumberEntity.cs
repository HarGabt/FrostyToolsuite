using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes
{
    public abstract class BaseNumberNode : EntityNode
    {
        public override string ToolTip => "This node stores a number value which can be incremented and decremented";

        public override void BuildFooter()
        {
            Footer = $"Default Value: {TryGetProperty("DefaultValue")}\nAdjustment Value: {TryGetProperty("IncDecValue")}";
        }

        public BaseNumberNode()
        {
            Inputs = new ObservableCollection<IPort>
            {
                new EventInput("Increment", this),
                new EventInput("Decrement", this),
                new EventInput("Reset", this),
            };

            Outputs = new ObservableCollection<IPort>
            {
                new PropertyOutput("Value", this)
            };
        }
    }
}