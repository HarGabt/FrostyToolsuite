using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace BlueprintEditorPlugin.Views.Buttons
{
    /// <summary>
    /// A button which when pressed drops down a context menu
    /// </summary>
    public class DropDownButton : ToggleButton
    {
        /// <summary>
        /// The menu to display when this button is toggled
        /// </summary>
        public ContextMenu Menu
        {
            get { return (ContextMenu)GetValue(MenuProperty); }
            set { SetValue(MenuProperty, value); }
        }
        public static readonly DependencyProperty MenuProperty = DependencyProperty.Register("Menu",
            typeof(ContextMenu), typeof(DropDownButton), new UIPropertyMetadata(null, OnMenuChanged));
        
        public DropDownButton()
        {
            Binding binding = new Binding("Menu.IsOpen")
            {
                Source = this
            };
            SetBinding(IsCheckedProperty, binding);
            
            DataContextChanged += (sender, args) =>
            {
                if (Menu != null)
                    Menu.DataContext = DataContext;
            };
        }

        private static void OnMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropDownButton dropDownButton = (DropDownButton)d;
            ContextMenu contextMenu = (ContextMenu)e.NewValue;
            contextMenu.DataContext = dropDownButton.DataContext;
        }

        protected override void OnClick()
        {
            if (Menu == null)
                return;

            Menu.PlacementTarget = this;
            Menu.Placement = PlacementMode.Bottom;
            Menu.IsOpen = true;
        }
    }
}