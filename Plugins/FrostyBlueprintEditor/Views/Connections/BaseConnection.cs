using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BlueprintEditorPlugin.Views.Editor;
using BlueprintEditorPlugin.Views.Events;
using BlueprintEditorPlugin.Views.Helpers;
using BlueprintEditorPlugin.Views.Wires;

namespace BlueprintEditorPlugin.Views.Connections
{
    /// <summary>
    /// Represents the base class for shapes that are drawn from a <see cref="BaseWire.Source"/> point to a <see cref="BaseWire.Target"/> point.
    /// </summary>
    public abstract class BaseConnection : BaseWire
    {
        #region Dependency Properties
        
        public static readonly DependencyProperty SplitCommandProperty = DependencyProperty.Register(nameof(SplitCommand), typeof(ICommand), typeof(BaseConnection));
        public static readonly DependencyProperty DisconnectCommandProperty = Connector.DisconnectCommandProperty.AddOwner(typeof(BaseConnection));

        /// <summary>
        /// Splits the connection. Triggered by <see cref="EditorGestures.Connection.Split"/> gesture.
        /// Parameter is the location where the splitting ocurred.
        /// </summary>
        public ICommand SplitCommand
        {
            get => (ICommand)GetValue(SplitCommandProperty);
            set => SetValue(SplitCommandProperty, value);
        }

        /// <summary>
        /// Removes this connection. Triggered by <see cref="EditorGestures.Connection.Disconnect"/> gesture.
        /// Parameter is the location where the disconnect ocurred.
        /// </summary>
        public ICommand DisconnectCommand
        {
            get => (ICommand)GetValue(DisconnectCommandProperty);
            set => SetValue(DisconnectCommandProperty, value);
        }

        #endregion

        #region Routed Events

        public static readonly RoutedEvent DisconnectEvent = EventManager.RegisterRoutedEvent(nameof(Disconnect), RoutingStrategy.Bubble, typeof(ConnectionEventHandler), typeof(BaseConnection));
        public static readonly RoutedEvent SplitEvent = EventManager.RegisterRoutedEvent(nameof(Split), RoutingStrategy.Bubble, typeof(ConnectionEventHandler), typeof(BaseConnection));

        /// <summary>Triggered by the <see cref="EditorGestures.Connection.Disconnect"/> gesture.</summary>
        public event ConnectionEventHandler Disconnect
        {
            add => AddHandler(DisconnectEvent, value);
            remove => RemoveHandler(DisconnectEvent, value);
        }

        /// <summary>Triggered by the <see cref="EditorGestures.Connection.Split"/> gesture.</summary>
        public event ConnectionEventHandler Split
        {
            add => AddHandler(SplitEvent, value);
            remove => RemoveHandler(SplitEvent, value);
        }

        #endregion

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            Focus();

            this.CaptureMouseSafe();

            if (EditorGestures.Connection.Split.Matches(e.Source, e) && (SplitCommand?.CanExecute(this) ?? false))
            {
                Point splitLocation = e.GetPosition(this);
                object connection = DataContext;
                var args = new ConnectionEventArgs(connection)
                {
                    RoutedEvent = SplitEvent,
                    SplitLocation = splitLocation,
                    Source = this
                };

                RaiseEvent(args);

                // Raise SplitCommand if SplitEvent is not handled
                if (!args.Handled && (SplitCommand?.CanExecute(splitLocation) ?? false))
                {
                    SplitCommand.Execute(splitLocation);
                }

                e.Handled = true;
            }
            else if (EditorGestures.Connection.Disconnect.Matches(e.Source, e) && (DisconnectCommand?.CanExecute(this) ?? false))
            {
                Point splitLocation = e.GetPosition(this);
                object connection = DataContext;
                var args = new ConnectionEventArgs(connection)
                {
                    RoutedEvent = DisconnectEvent,
                    SplitLocation = splitLocation,
                    Source = this
                };

                RaiseEvent(args);

                // Raise DisconnectCommand if DisconnectEvent is not handled
                if (!args.Handled && (DisconnectCommand?.CanExecute(splitLocation) ?? false))
                {
                    DisconnectCommand.Execute(splitLocation);
                }

                e.Handled = true;
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }
        }
    }
}
