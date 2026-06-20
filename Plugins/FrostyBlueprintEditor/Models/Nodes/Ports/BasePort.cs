using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Models.Nodes.Utilities;

namespace BlueprintEditorPlugin.Models.Nodes.Ports
{
    public abstract class BasePort : IPort
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyPropertyChanged(nameof(Name));
            }
        }

        public abstract PortDirection Direction { get; }

        private Point _anchor;
        public Point Anchor
        {
            get => _anchor;
            set
            {
                _anchor = value;
                NotifyPropertyChanged(nameof(Anchor));
            }
        }
        public INode Node { get; protected set; }
        public IRedirect RedirectNode { get; set; }

        private bool _isConnected;

        public bool IsConnected
        {
            set
            {
                _isConnected = value;
                NotifyPropertyChanged(nameof(IsConnected));
            }
            get => _isConnected;
        }
        
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public BasePort(string name, INode node)
        {
        }
        
        public override string ToString()
        {
            return $"{Direction}put - {Name}";
        }
    }
    public class BaseInput : BasePort
    {
        public override PortDirection Direction => PortDirection.In;

        public BaseInput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }
    
    public class BaseOutput : BasePort
    {
        public override PortDirection Direction => PortDirection.Out;

        public BaseOutput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }
}