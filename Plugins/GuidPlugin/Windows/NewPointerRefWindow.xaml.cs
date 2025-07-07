using Frosty.Controls;
using System;
using System.Windows;
using System.Windows.Controls;

namespace GuidPlugin.Windows
{
    /// <summary>
    /// Interaction logic for DuplicateAssetWindow.xaml
    /// </summary>
    public partial class NewPointerRefWindow : FrostyDockableWindow
    {
        public Guid SelectedRootGUID { get; private set; } = new Guid();
        public Guid SelectedFileGUID { get; private set; } = new Guid();

        public NewPointerRefWindow()
        {
            InitializeComponent();
            assetInstanceGuidTextBox.Text = SelectedRootGUID.ToString();
            assetFileGuidTextBox.Text = SelectedFileGUID.ToString();
        }

        private void AssetGuidTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox source = e.OriginalSource as TextBox;
            try
            {
                new Guid(source.Text);
            }
            catch
            {
                if (source == assetInstanceGuidTextBox)
                    source.Text = SelectedRootGUID.ToString();
                else if (source == assetFileGuidTextBox)
                    source.Text = SelectedFileGUID.ToString();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedRootGUID = new Guid(assetInstanceGuidTextBox.Text);
            SelectedFileGUID = new Guid(assetFileGuidTextBox.Text);

            DialogResult = true;
            Close();
        }
    }
}
