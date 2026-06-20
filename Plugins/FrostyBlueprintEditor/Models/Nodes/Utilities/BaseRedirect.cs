using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.IO;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Models.Nodes.Utilities
{
    public abstract class BaseRedirect : IRedirect
    {
        #region Node implementation

        #region Vertex Implementation

        private Point _location;
        public Point Location
        {
            get => _location;
            set
            {
                _location = value;
                NotifyPropertyChanged(nameof(Location));
            }
        }

        private Size _size;
        public Size Size
        {
            get => _size;
            set
            {
                _size = value;
                NotifyPropertyChanged(nameof(Size));
            }
        }
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }
        public INodeWrangler NodeWrangler { get; protected set; }

        #endregion

        #region Basic implementation

        private string _header;
        public string Header
        {
            get => _header;
            set
            {
                _header = value;
                NotifyPropertyChanged(nameof(Header));
            }
        }

        public ObservableCollection<IPort> Inputs { get; } = new ObservableCollection<IPort>();
        public ObservableCollection<IPort> Outputs { get; } = new ObservableCollection<IPort>();
        
        public bool IsValid()
        {
            return true;
        }

        public void OnCreation()
        {
        }

        public virtual void OnDestruction()
        {
            if (Direction == PortDirection.In)
            {
                foreach (IConnection connection in NodeWrangler.GetConnections(Inputs[0]))
                {
                    if (connection.Target != Inputs[0])
                        return;
                    
                    connection.Target = RedirectTarget;
                }
            }
            else
            {
                IConnection connection = NodeWrangler.GetConnections(Outputs[0]).FirstOrDefault();
                if (connection == null)
                    return;
                
                NodeWrangler.RemoveConnection(connection);
            }
        }

        public void OnInputUpdated(IPort port)
        {
        }

        public void OnOutputUpdated(IPort port)
        {
        }

        public void AddPort(IPort port)
        {
        }
        
        public void RemovePort(IPort port)
        {
        }

        #endregion

        #endregion

        #region Redirect Implementation

        public PortDirection Direction { get; set; }

        private IRedirect _source;
        public IRedirect SourceRedirect
        {
            get => _source;
            set
            {
                _source = value;
            }
        }

        private IRedirect _target;

        public IRedirect TargetRedirect
        {
            get => _target;
            set
            {
                _target = value;
            }
        }
        public IPort RedirectTarget { get; set; }

        #endregion

        public abstract bool Load(LayoutReader reader);

        public abstract void Save(LayoutWriter writer);
        
        #region Property Changing

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        /// <summary>
        /// Keep watch of <see cref="RedirectTarget"/> updates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void RedirectTargetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Name":
                {
                    if (Direction == PortDirection.Out)
                    {
                        Outputs[0].Name = RedirectTarget.Name;
                    }
                    else
                    {
                        Inputs[0].Name = RedirectTarget.Name;
                    }
                } break;
                case "Node":
                {
                    App.Logger.LogError("Fuck! Someone tell ywingpilot2 that the port node changed on a redirect...");
                    throw new NotImplementedException("Fuck! Someone tell ywingpilot2 that the port node changed on a redirect...");
                }
            }
        }

        protected virtual void OurPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        #endregion

        public BaseRedirect(IPort redirectTarget, PortDirection direction, INodeWrangler wrangler)
        {
            RedirectTarget = redirectTarget;
            Direction = direction;

            RedirectTarget.PropertyChanged += RedirectTargetPropertyChanged;
            RedirectTarget.Node.PropertyChanged += RedirectTargetPropertyChanged;
            NodeWrangler = wrangler;
        }

        protected BaseRedirect()
        {
        }

        public override string ToString()
        {
            if (Header == null)
            {
                return $"Redirect of {RedirectTarget}";
            }

            return $"Redirect {Header} of {RedirectTarget}";
        }
    }
}