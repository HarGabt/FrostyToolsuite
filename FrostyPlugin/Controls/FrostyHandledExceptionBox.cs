using Frosty.Controls;
using System.Windows;

namespace Frosty.Core.Controls
{
    public class FrostyHandledExceptionBox : FrostyDockableWindow
    {
        #region -- Properties --

        #region -- WarningText --
        public static readonly DependencyProperty WarningTextProperty = DependencyProperty.Register("WarningText", typeof(string), typeof(FrostyHandledExceptionBox), new PropertyMetadata(""));
        public string WarningText {
            get => (string)GetValue(WarningTextProperty);
            set => SetValue(WarningTextProperty, value);
        }
        #endregion

        #endregion

        public FrostyHandledExceptionBox()
        {
            Title = "Warning";
            Topmost = true;
            ShowInTaskbar = false;

            Height = 180;
            Width = 400;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Window mainWin = Application.Current.MainWindow;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        public static MessageBoxResult Show(string warning)
        {
            FrostyHandledExceptionBox window = new FrostyHandledExceptionBox
            {
                WarningText = warning
            };

            return (window.ShowDialog() == true) ? MessageBoxResult.OK : MessageBoxResult.Cancel;
        }
    }
}