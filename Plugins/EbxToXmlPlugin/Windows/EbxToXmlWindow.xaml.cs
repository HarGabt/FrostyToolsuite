using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using FrostySdk.Managers.Entries;

namespace EbxToXmlPlugin.Windows
{
    /// <summary>
    /// Interaction logic for DuplicateAssetWindow.xaml
    /// </summary>
    public partial class EbxToXmlWindow : FrostyDockableWindow
    {

        public EbxToXmlWindow()
        {
            InitializeComponent();
        }

        private void FrostyDockableWindow_FrostyLoaded(object sender, EventArgs e)
        {
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
