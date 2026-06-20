using System.Windows;
using Frosty.Controls;

namespace BlueprintEditorPlugin.Windows
{
    public partial class EditPromptWindow : FrostyDockableWindow
    {
        public MessageBoxResult Result { get; internal set; }

        public EditPromptWindow(object obj)
        {
            Result = MessageBoxResult.Cancel;
            InitializeComponent();
            PropertyGrid.Object = obj;
        }

        /// <summary>
        /// Show a property edit prompt. Do note, the object's properties will be edited directly, regardless of the users actions.
        /// </summary>
        /// <param name="args">The object whom'st properties shalt be shown.</param>
        /// <param name="title">The title of the window</param>
        /// <returns>Confirmation whether the user wants to cancel or proceed</returns>
        public static MessageBoxResult Show(object args, string title = "Edit Properties")
        {
            EditPromptWindow editPromptWindow = new EditPromptWindow(args)
            {
                Title = title
            };
            editPromptWindow.ShowDialog();
            
            return editPromptWindow.Result;
        }

        public static MessageBoxResult Show(object args, ResizeMode resizeMode, string title = "Edit Properties")
        {
            EditPromptWindow editPromptWindow = new EditPromptWindow(args)
            {
                Title = title,
                ResizeMode = resizeMode
            };
            editPromptWindow.ShowDialog();
            
            return editPromptWindow.Result;
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            Close();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}