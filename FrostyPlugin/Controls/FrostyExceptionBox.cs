using Frosty.Controls;
using Frosty.Core.Windows;
using SharpDX;
using System;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Frosty.Core.Controls
{
    public class ExceptionBoxClickCommand : System.Windows.Input.ICommand
    {
        public event EventHandler CanExecuteChanged {
            add => System.Windows.Input.CommandManager.RequerySuggested += value;
            remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            System.Windows.Controls.Button btn = parameter as System.Windows.Controls.Button;
            FrostyExceptionBox parentWin = Window.GetWindow(btn) as FrostyExceptionBox;

            string buttonName = btn.Name;

            if (buttonName == "PART_CopyExceptionButton")
            {
                Clipboard.SetText(parentWin.ExceptionText);
                Clipboard.Flush();
            }
            else if (buttonName == "PART_CopyLogButton")
            {
                Clipboard.SetText(parentWin.LogText);
                Clipboard.Flush();
            }
        }
    }

    public class FrostyExceptionBox : FrostyDockableWindow
    {
        #region -- Properties --

        #region -- Text --
        public static readonly DependencyProperty ExceptionTextProperty = DependencyProperty.Register("ExceptionText", typeof(string), typeof(FrostyExceptionBox), new PropertyMetadata(""));
        public string ExceptionText
        {
            get => (string)GetValue(ExceptionTextProperty);
            set => SetValue(ExceptionTextProperty, value);
        }

        public static readonly DependencyProperty ExceptionMessageTextProperty = DependencyProperty.Register("ExceptionMessageText", typeof(string), typeof(FrostyExceptionBox), new PropertyMetadata(""));
        public string ExceptionMessageText {
            get => (string)GetValue(ExceptionMessageTextProperty);
            set => SetValue(ExceptionMessageTextProperty, value);
        }

        public static readonly DependencyProperty LogTextProperty = DependencyProperty.Register("LogText", typeof(string), typeof(FrostyExceptionBox), new PropertyMetadata(""));
        public string LogText {
            get => (string)GetValue(LogTextProperty);
            set => SetValue(LogTextProperty, value);
        }
        #endregion

        #endregion

        public FrostyExceptionBox()
        {
            Topmost = true;
            ShowInTaskbar = false;

            Height = 600;
            Width = 900;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Window mainWin = Application.Current.MainWindow;

            if (mainWin != null)
            {
                Icon = mainWin.Icon;

                double x = mainWin.Left + (mainWin.Width / 2.0);
                double y = mainWin.Top + (mainWin.Height / 2.0);

                Left = x - (Width / 2.0);
                Top = y - (Height / 2.0);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        public static MessageBoxResult Show(Exception e, string title)
        {
            FrostyExceptionBox window = new FrostyExceptionBox
            {
                Title = title,
                ExceptionText = UnlocalizeException(e),
                LogText = (App.Logger as FrostyCore.FrostyLogger).LogText,
                ExceptionMessageText = e.Message
            };

            // Write crash log
            try
            {
                Directory.CreateDirectory($"{Environment.CurrentDirectory}\\CrashLogs");
                using (StreamWriter writer = new StreamWriter(new FileStream($"{Environment.CurrentDirectory}\\CrashLogs\\{DateTime.Now:ddMMyyyy_HHmmss}_{e.Source}.txt", FileMode.Create)))
                {
                    writer.WriteLine("[Exception]");
                    writer.WriteLine(window.ExceptionText);
                    writer.WriteLine("[Log]");
                    writer.Write(window.LogText);
                }
            }
            catch (IOException)
            {
                using (StreamWriter writer = new StreamWriter(new FileStream($"crashlog_{DateTime.Now:ddMMyyyy_HHmmss}_{e.Source}.txt", FileMode.Create)))
                {
                    writer.WriteLine("[Exception]");
                    writer.WriteLine(window.ExceptionText);
                    writer.WriteLine("[Log]");
                    writer.Write(window.LogText);
                }
            }
            catch
            {
                App.Logger.LogError("Failed to write crash log");
            }

            return (window.ShowDialog() == true) ? MessageBoxResult.OK : MessageBoxResult.Cancel;
        }

        /// <summary>
        /// Try to generate exception message in English
        /// </summary>
        private static string UnlocalizeException(Exception ex)
        {
            try
            {
                // Call UnlocalizedExceptionGenerator to get exception message in English
                UnlocalizedExceptionGenerator ueg = new UnlocalizedExceptionGenerator(ex, System.Threading.Thread.CurrentThread.CurrentUICulture);
                System.Threading.Thread thread = new System.Threading.Thread(ueg.Run)
                {
                    CurrentCulture = CultureInfo.InvariantCulture,
                    CurrentUICulture = CultureInfo.InvariantCulture
                };
                thread.Start();
                thread.Join();

                return ueg.ExceptionDetails;
            }
            catch
            {
                App.Logger.LogError("Failed to translate exception");

                StringBuilder sb = new StringBuilder();
                sb.Append("Type=");
                sb.AppendLine(ex.GetType().ToString());
                sb.Append("HResult=");
                sb.AppendLine("0x" + ex.HResult.ToString("X"));
                sb.Append("Message=");
                sb.AppendLine(ex.Message);
                sb.Append("Source=");
                sb.AppendLine(ex.Source);
                sb.AppendLine("StackTrace:");
                sb.AppendLine(ex.StackTrace);
                return sb.ToString();
            }
        }

        /// <summary>
        /// Used to generate an exception message in English
        /// https://stackoverflow.com/questions/209133/exception-messages-in-english
        /// </summary>
        private class UnlocalizedExceptionGenerator
        {
            private Exception _ex;
            private CultureInfo _origCultureInfo;

            public string ExceptionDetails;

            public UnlocalizedExceptionGenerator(Exception ex, CultureInfo origCultureInfo)
            {
                _ex = ex;
                _origCultureInfo = origCultureInfo;
            }

            public void Run()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Type=");
                sb.AppendLine(_ex.GetType().ToString());
                sb.Append("HResult=");
                sb.AppendLine("0x" + _ex.HResult.ToString("X"));
                sb.Append("Message=");

                // Find message in .net localize resources
                string exMessage = _ex.Message;
                try
                {
                    System.Reflection.Assembly assembly = _ex.GetType().Assembly;
                    ResourceManager rm = new ResourceManager(assembly.GetName().Name, assembly);
                    ResourceSet originalResources = rm.GetResourceSet(_origCultureInfo, true, true);
                    ResourceSet targetResources = rm.GetResourceSet(CultureInfo.InvariantCulture, true, true);

                    foreach (System.Collections.DictionaryEntry originalResource in originalResources)
                    {
                        if (!(originalResource.Value is string message))
                            continue;

                        string translate = targetResources.GetString(originalResource.Key.ToString(), false);

                        if (!message.Contains("{"))
                        {
                            exMessage = exMessage.Replace(message, translate);
                        }
                        else
                        {
                            string pattern = $"{Regex.Escape(message)}";
                            pattern = Regex.Replace(pattern, @"\\{([0-9]+)\}", "(?<group$1>.*)");

                            Regex regex = new Regex(pattern);

                            string replacePattern = translate;
                            replacePattern = Regex.Replace(replacePattern, @"{([0-9]+)}", @"${group$1}");
                            replacePattern = replacePattern.Replace("\\$", "$");

                            exMessage = regex.Replace(exMessage, replacePattern);
                        }
                    }
                }
                catch 
                {
                    App.Logger.LogError("Failed to translate exception message");
                }
                sb.AppendLine(exMessage);

                sb.Append("Source=");
                sb.AppendLine(_ex.Source);
                sb.AppendLine("StackTrace:");
                sb.AppendLine(_ex.StackTrace);

                ExceptionDetails = sb.ToString();
            }
        }
    }
}
