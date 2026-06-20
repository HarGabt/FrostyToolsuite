using System.Windows;
using System.Windows.Controls;
using BlueprintEditorPlugin.Views.Connections;

namespace BlueprintEditorPlugin.Views.Nodes
{
    /// <summary>
    /// Represents a control that owns a <see cref="Connector"/>.
    /// </summary>
    public class KnotNode : ContentControl
    {
        static KnotNode()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(KnotNode), new FrameworkPropertyMetadata(typeof(KnotNode)));
        }
    }
}
