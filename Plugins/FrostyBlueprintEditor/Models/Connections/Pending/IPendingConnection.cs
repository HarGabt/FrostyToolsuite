using System.ComponentModel;
using System.Windows.Input;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Models.Connections.Pending
{
    public interface IPendingConnection : INotifyPropertyChanged
    {
        IPort Source { get; set; }

        ICommand Start { get; }
        ICommand Finish { get; }
    }
}