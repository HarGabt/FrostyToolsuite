using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Options;
using Prism.Commands;

namespace BlueprintEditorPlugin.Models.Connections.Pending
{
    public class BasePendingConnection : IPendingConnection
    {
        public IPort Source { get; set; }
        
        #region Anchors

        private Point _sAnchor;
        public Point SourceAnchor
        {
            get => _sAnchor;
            set
            {
                _sAnchor = value;
                NotifyPropertyChanged(nameof(SourceAnchor));
                NotifyPropertyChanged(nameof(CurvePoint1));
            }
        }

        private Point _tAnchor;
        public Point TargetAnchor
        {
            get => _tAnchor;
            set
            {
                _tAnchor = value;
                NotifyPropertyChanged(nameof(TargetAnchor));
                NotifyPropertyChanged(nameof(CurvePoint2));
            }
        }

        #endregion

        #region Curve Points

        public Point CurvePoint1
        {
            get
            {
                //The curve point is just the average of the 2 points
                if (Source != null && Source.Direction == PortDirection.Out)
                {
                    if (EditorOptions.WireStyle == ConnectionStyle.Curvy)
                    {
                        //The curve point is just the average of the 2 points
                        return new Point(SourceAnchor.X + 85,
                            Source.Anchor.Y);
                    }
                    else
                    {
                        return new Point(SourceAnchor.X + 25,
                            Source.Anchor.Y);
                    }
                }
                else
                {
                    if (EditorOptions.WireStyle == ConnectionStyle.Curvy)
                    {
                        //The curve point is just the average of the 2 points
                        return new Point(TargetAnchor.X - 85,
                            TargetAnchor.Y);
                    }
                    else
                    {
                        return new Point(TargetAnchor.X - 25,
                            TargetAnchor.Y);
                    }
                }
            }
        }

        public Point CurvePoint2
        {
            get
            {
                //The curve point is just the average of the 2 points
                if (Source != null && Source.Direction == PortDirection.Out)
                {
                    if (EditorOptions.WireStyle == ConnectionStyle.Curvy)
                    {
                        //The curve point is just the average of the 2 points
                        return new Point(TargetAnchor.X - 85,
                            TargetAnchor.Y);
                    }
                    else
                    {
                        return new Point(TargetAnchor.X - 25,
                            TargetAnchor.Y);
                    }
                }
                else
                {
                    if (EditorOptions.WireStyle == ConnectionStyle.Curvy)
                    {
                        //The curve point is just the average of the 2 points
                        return new Point(TargetAnchor.X + 85,
                            TargetAnchor.Y);
                    }
                    else
                    {
                        return new Point(TargetAnchor.X + 25,
                            TargetAnchor.Y);
                    }
                }
            }
        }

        #endregion

        public ICommand Start { get; protected set; }
        public ICommand Finish { get; protected set; }

        public BasePendingConnection(INodeWrangler wrangler)
        {
            Start = new DelegateCommand<IPort>(source =>
            {
                Source = source;
                NotifyPropertyChanged(nameof(CurvePoint1));
                NotifyPropertyChanged(nameof(CurvePoint2));
            });
            Finish = new DelegateCommand<IPort>(target =>
            {
                if (target == null)
                    return;
                
                if (Source.Direction == PortDirection.Out && target.Direction == PortDirection.In)
                {
                    wrangler.AddConnection(new BaseConnection((BaseOutput)Source, (BaseInput)target));
                }
                else if (Source.Direction == PortDirection.In && target.Direction == PortDirection.Out)
                {
                    wrangler.AddConnection(new BaseConnection((BaseOutput)target, (BaseInput)Source));
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}