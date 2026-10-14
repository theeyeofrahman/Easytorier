using System.Windows;
using System.Windows.Input;

namespace Easytorier
{
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();

            // Allow dragging the window around since WindowStyle is None
            MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                    DragMove();
            };
        }

        private void OnStartEditorClick(object sender, RoutedEventArgs e)
        {
            var mainEditor = new MainWindow();
            mainEditor.Show();
            Close();
        }

        private void OnOpenProjectsClick(object sender, RoutedEventArgs e)
        {
            var projectManager = new ProjectManagerWindow();
            projectManager.ShowDialog();
        }

        private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
        {
            // Open settings using a temporary instance or launch editor directly
            var mainEditor = new MainWindow();
            mainEditor.Show();
            Close();
        }

        private void OnOpenMinigameClick(object sender, RoutedEventArgs e)
        {
            var mainEditor = new MainWindow();
            mainEditor.Show();
            // Optional: call theme/minigame window right on launch
            Close();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}