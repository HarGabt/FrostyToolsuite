using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BlueprintEditorPlugin.Models.Status
{
    public enum EditorStatus
    {
        Alright = 0,
        Flawed = 1,
        Broken = 2
    }

    public class EditorStatusArgs : INotifyPropertyChanged
    {
        private EditorStatus _status;
        private string _toolTip;

        public EditorStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                NotifyPropertyChanged(nameof(Status));
            }
        }

        public string ToolTip
        {
            get => _toolTip;
            set
            {
                _toolTip = value;
                NotifyPropertyChanged(nameof(ToolTip));
            }
        }

        public EditorStatusArgs(EditorStatus status, string tooltip = "")
        {
            Status = status;
            ToolTip = tooltip;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}