using Frosty.Controls;
using FrostySdk;
using System.Windows;

namespace Frosty.Core.Windows
{
    /// <summary>
    /// Interaction logic for KeyPromptWindow.xaml
    /// </summary>
    public partial class KeyPromptWindow : FrostyDockableWindow
    {
        public byte[] EncryptionKey;

        public KeyPromptWindow()
        {
            InitializeComponent();
        }

        private void doneButton_Click(object sender, RoutedEventArgs e)
        {
            string encryptionKey = keyTextBox.Text.Trim();

            EncryptionKey = new byte[encryptionKey.Length / 2];

            try
            {
                for (int i = 0; i < encryptionKey.Length / 2; i++)
                {
                    EncryptionKey[i] = byte.Parse(encryptionKey.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                }
            }
            catch
            {
                FrostyMessageBox.Show("Encryption key is invalid. Please try again.", "Frosty Core");
                return;
            }

                DialogResult = true;
                Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
