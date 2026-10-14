using System.Windows;
using System.Windows.Input;

namespace Easytorier
{
    public partial class MinigameEditorWindow : Window
    {
        public MinigameEditorWindow()
        {
            InitializeComponent();
            MouseDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) DragMove(); };
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Arcade rules updated!", "Easytorier", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}