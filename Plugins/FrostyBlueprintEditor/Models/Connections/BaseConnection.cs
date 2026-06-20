using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;
using BlueprintEditorPlugin.Options;

namespace BlueprintEditorPlugin.Models.Connections
{
    public class BaseConnection : IConnection, IStatusItem
    {
        private IPort _source;
        public IPort Source
        {
            get => _source;
            set
            {
                //Unsubscribed! L! Cope! Seethe!
                if (_source != null)
                {
                    _source.PropertyChanged -= NotifyPropertyChanged;
                    _source.Node.PropertyChanged -= NotifyPropertyChanged;
                    
                    if (!_source.Node.NodeWrangler.GetConnections(_source).Any())
                    {
                        _source.IsConnected = false;
                    }
                }
                
                //Subscribed! Liked! W!
                value.PropertyChanged += NotifyPropertyChanged;
                value.Node.PropertyChanged += NotifyPropertyChanged;
                
                _source = value;
                _source.IsConnected = true;
                NotifyPropertyChanged(nameof(Source));
            }
        }

        private IPort _target;
        public IPort Target
        {
            get => _target;
            set
            {
                //Unsubscribed! L! Cope! Seethe!
                if (_target != null)
                {
                    _target.PropertyChanged -= NotifyPropertyChanged;
                    _target.Node.PropertyChanged -= NotifyPropertyChanged;
                    
                    if (!_target.Node.NodeWrangler.GetConnections(_target).Any())
                    {
                        _target.IsConnected = false;
                    }
                }
                
                //Subscribed! Liked! W!
                value.PropertyChanged += NotifyPropertyChanged;
                value.Node.PropertyChanged += NotifyPropertyChanged;
                
                _target = value;
                _target.IsConnected = true;
                NotifyPropertyChanged(nameof(Target));
            }
        }
        
        public bool IsSelected { get; set; }

        #region Property changing

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        /// <summary>
        /// Basically just a way to update CurvePoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void NotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsSelected":
                {
                    IsSelected = Source.Node.IsSelected || Target.Node.IsSelected;
                    NotifyPropertyChanged(nameof(IsSelected));
                } break;
            }
        }

        #endregion

        #region Status

        public EditorStatusArgs CurrentStatus { get; set; }
        public virtual void CheckStatus()
        {
            NotifyPropertyChanged(nameof(CurrentStatus));
        }

        public virtual void UpdateStatus()
        {
            CurrentStatus = new EditorStatusArgs(EditorStatus.Alright, "");
            
            if (Source == Target)
            {
                CurrentStatus = new EditorStatusArgs(EditorStatus.Broken, "Cannot connect a port to itself");
            }

            if (Source.Direction != PortDirection.Out)
            {
                CurrentStatus = new EditorStatusArgs(EditorStatus.Broken, "Source has to be an output");
            }
            
            if (Target.Direction != PortDirection.In)
            {
                CurrentStatus = new EditorStatusArgs(EditorStatus.Broken, "Target has to be an input");
            }
            
            if (Target.Direction != PortDirection.In)
            {
                CurrentStatus = new EditorStatusArgs(EditorStatus.Broken, "Target has to be an input");
            }

            if (Source.Direction == Target.Direction)
            {
                CurrentStatus = new EditorStatusArgs(EditorStatus.Broken, "An output has to go to an input");
            }
            
            CheckStatus();
        }

        public virtual void SetStatus(EditorStatus status, string tooltip)
        {
            CurrentStatus.Status = status;
            CurrentStatus.ToolTip = tooltip;
            CheckStatus();
        }

        #endregion

        protected BaseConnection()
        {
        }
        
        public BaseConnection(IPort source, IPort target)
        {
            Source = source;
            Target = target;

            CurrentStatus = new EditorStatusArgs(EditorStatus.Alright);
        }
        
        public override string ToString()
        {
            return $"{Source.Node}({Source.Name}) -> {Target.Node}({Target.Name})";
        }
    }
}