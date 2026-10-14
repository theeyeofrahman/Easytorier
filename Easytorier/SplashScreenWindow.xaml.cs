using System.Windows;

namespace Easytorier
{
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
            ThemeManager.Apply(ThemeManager.LoadActive());
        }

        private void OnStartEditorClick(object sender, RoutedEventArgs e)
        {
            // Launch main editor and close splash
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private void OnOpenProjectsClick(object sender, RoutedEventArgs e)
        {
            // Hide the welcome screen while the project browser is open.
            Hide();
            var projectManager = new ProjectManagerWindow { Owner = this };

            // If project manager closes without opening a project, show splash again
            bool? result = projectManager.ShowDialog();
            if (result != true)
            {
                Show();
            }
            else
            {
                Close();
            }
        }

        private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
        {
            new ThemeEditorWindow { Owner = this }.ShowDialog();
        }

        private void OnOpenMinigameClick(object sender, RoutedEventArgs e)
        {
            new MinigameEditorWindow { Owner = this }.ShowDialog();
        }

        private void OnOpenCreditsClick(object sender, RoutedEventArgs e)
        {
            new CreditsWindow { Owner = this }.ShowDialog();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}