using Frosty.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EffectBlueprintEditorPlugin.Windows {
    /// <summary>
    /// Interaction logic for NewParamWindow.xaml
    /// </summary>
    public partial class NewParamWindow : FrostyDockableWindow {

        public Dictionary<int, string> Parameters = new Dictionary<int, string>();

        public int SelectedParam { get; set; }

        public NewParamWindow(Dictionary<int, string[]> vsfParams, dynamic currentVsf) {
            InitializeComponent();

            foreach (KeyValuePair<int, string[]> param in vsfParams) {
                List<int> currentIds = new List<int>();
                foreach (dynamic p in currentVsf)
                    currentIds.Add(p.PropertyId);

                if (!currentIds.Contains(param.Key))
                    Parameters.Add(param.Key, String.Join("_", param.Value));
            }

            ParamComboBox.ItemsSource = Parameters.ToList();
            ParamComboBox.DisplayMemberPath = "Value";

            if (Parameters.Count > 0)
                ParamComboBox.SelectedIndex = 0;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            SelectedParam = ((KeyValuePair<int, string>)ParamComboBox.SelectedItem).Key;

            DialogResult = true;
            Close();
        }
    }
}
