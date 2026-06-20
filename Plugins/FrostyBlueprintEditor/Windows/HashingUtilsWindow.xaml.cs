using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Frosty.Controls;
using FrostySdk;

namespace BlueprintEditorPlugin.Windows
{
    public partial class HashingUtilsWindow : FrostyDockableWindow
    {
        public HashingUtilsWindow()
        {
            InitializeComponent();
        }

        private void HexText_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (int.TryParse(HexText.Text.Replace("0x", ""), NumberStyles.AllowHexSpecifier, new NumberFormatInfo(),
                    out int hash))
            {
                StringText.WatermarkText = "";
                StringText.Text = Utils.GetString(hash);
                IntText.WatermarkText = "";
                IntText.Text = hash.ToString();
            }
        }

        private void IntText_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (int.TryParse(IntText.Text, out int hash))
            {
                HexText.WatermarkText = "";
                HexText.Text = $"0x{hash:x8}";
                StringText.WatermarkText = "";
                StringText.Text = Utils.GetString(hash);
            }
        }

        private void StringText_OnKeyUp(object sender, KeyEventArgs e)
        {
            int hash = Utils.HashString(StringText.Text);
            IntText.WatermarkText = "";
            IntText.Text = hash.ToString();
            HexText.WatermarkText = "";
            HexText.Text = $"0x{hash:x8}";
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}