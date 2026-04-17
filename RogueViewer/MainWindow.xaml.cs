using RogueBot;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RogueViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var vm = (MainViewModel)DataContext;

            if (vm?.SelectedInstance == null)
                return;

            // Convert WPF Key → char/string
            string key = KeyToString(e.Key);

            if (!string.IsNullOrEmpty(key))
            {
                vm.SelectedInstance.SendKey(key);
                e.Handled = true;
            }
        }

        private string KeyToString(Key key)
        {
            // Letters
            if (key >= Key.A && key <= Key.Z)
                return key.ToString().ToLowerInvariant();

            // Numbers
            if (key >= Key.D0 && key <= Key.D9)
                return key.ToString().Replace("D", "");

            return key switch
            {
                Key.Space => C.Space.ToString(),
                Key.Enter => C.Enter.ToString(),
                Key.Left => C.Left.ToString(),
                Key.Right => C.Right.ToString(),
                Key.Up => C.Up.ToString(),
                Key.Down => C.Down.ToString(),
                Key.Escape => C.Escape.ToString(),
                Key.Multiply => "*",

                Key.OemPeriod => ".",
                Key.OemComma => ",",
                Key.OemMinus => "-",
                Key.OemPlus => "+",

                _ => ""
            };
        }
    }
}