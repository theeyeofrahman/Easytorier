using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Easytorier
{
    public partial class ProjectManagerWindow : Window
    {
        private static readonly string ProjectsFolderPath = Path.Combine(AppContext.BaseDirectory, "Projects");
        private const string UndeletableProject = "Vector";
        private string _selectedProject = string.Empty;
        private bool _isDescriptionDirty = false;
        private bool _refreshingList;

        public ProjectManagerWindow()
        {
            InitializeComponent();
            EnsureDirectories();
            RefreshProjectList();
            Closed += (_, _) => SaveDirtyDescription();
        }

        private void EnsureDirectories()
        {
            if (!Directory.Exists(ProjectsFolderPath))
                Directory.CreateDirectory(ProjectsFolderPath);

            string undeletablePath = Path.Combine(ProjectsFolderPath, UndeletableProject);
            if (!Directory.Exists(undeletablePath))
                Directory.CreateDirectory(undeletablePath);
        }

        private void RefreshProjectList()
        {
            SaveDirtyDescription();
            _refreshingList = true;
            try
            {
                ProjectListBox.Items.Clear();
                var query = ProjectSearchBox?.Text?.Trim() ?? string.Empty;
                if (Directory.Exists(ProjectsFolderPath))
                {
                    foreach (string dir in Directory.GetDirectories(ProjectsFolderPath)
                                 .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
                    {
                        string projName = Path.GetFileName(dir);
                        if (query.Length == 0 || projName.Contains(query, StringComparison.OrdinalIgnoreCase))
                            ProjectListBox.Items.Add(projName);
                    }
                }

                if (ProjectListBox.Items.Contains(_selectedProject))
                {
                    ProjectListBox.SelectedItem = _selectedProject;
                    DetailsPanel.IsEnabled = true;
                }
                else
                {
                    DetailsPanel.IsEnabled = false;
                }
            }
            finally { _refreshingList = false; }
        }

        private void ProjectSearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshProjectList();

        private void ProjectListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProjectListBox.SelectedItem is string) OnEditProjectClick(sender, e);
        }

        private void ProjectListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_refreshingList) return;
            if (ProjectListBox.SelectedItem is string projName)
            {
                SaveDirtyDescription();
                _selectedProject = projName;
                LoadProjectData(projName);
                DetailsPanel.IsEnabled = true;
            }
        }

        private void LoadProjectData(string projectName)
        {
            ProjectNameBox.Text = projectName;
            ProjectNameBox.IsEnabled = (projectName != UndeletableProject);

            string descPath = Path.Combine(ProjectsFolderPath, projectName, "description.txt");
            if (File.Exists(descPath))
            {
                ProjectDescBox.Text = File.ReadAllText(descPath);
            }
            else
            {
                ProjectDescBox.Text = string.Empty;
            }
            _isDescriptionDirty = false;
            var projectPath = Path.Combine(ProjectsFolderPath, projectName);
            var levelPath = Path.Combine(projectPath, "level.json");
            var modified = Directory.GetLastWriteTime(projectPath);
            ProjectMetaText.Text = $"Workspace: {projectPath}\nLast updated: {modified:g}\n{(File.Exists(levelPath) ? "Level file ready" : "No level saved yet")}";
            ProjectDescBox.IsEnabled = projectName != UndeletableProject;

            ThumbnailImage.Source = null;
            string[] extensions = { ".png", ".jpg", ".jpeg" };
            foreach (var ext in extensions)
            {
                string thumbPath = Path.Combine(ProjectsFolderPath, projectName, $"thumbnail{ext}");
                if (File.Exists(thumbPath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(Path.GetFullPath(thumbPath));
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        ThumbnailImage.Source = bitmap;
                        break;
                    }
                    catch { }
                }
            }
        }

        private void SaveDirtyDescription()
        {
            if (!_isDescriptionDirty || string.IsNullOrEmpty(_selectedProject)) return;
            try
            {
                string descPath = Path.Combine(ProjectsFolderPath, _selectedProject, "description.txt");
                File.WriteAllText(descPath, ProjectDescBox.Text);
                _isDescriptionDirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Couldn't save the project description.\n\n{ex.Message}", "Project Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProjectDescBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_refreshingList && !string.IsNullOrEmpty(_selectedProject)) _isDescriptionDirty = true;
        }

        private void ProjectDescBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveDirtyDescription();
        }

        private void ProjectNameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            RenameSelectedProject();
        }

        private void RenameSelectedProject()
        {
            if (string.IsNullOrEmpty(_selectedProject) || _selectedProject == UndeletableProject) return;

            string newName = ProjectNameBox.Text.Trim();
            if (string.IsNullOrEmpty(newName) || newName == _selectedProject) return;

            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                newName.Contains(Path.DirectorySeparatorChar) || newName.Contains(Path.AltDirectorySeparatorChar))
            {
                MessageBox.Show("Project names can't contain file path characters.", "Invalid name", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameBox.Text = _selectedProject;
                return;
            }

            if (newName.Equals("Vector", StringComparison.OrdinalIgnoreCase) ||
                newName.Equals("Editor", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"You cannot name a project '{newName}'!", "Action Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameBox.Text = _selectedProject;
                return;
            }

            string oldPath = Path.Combine(ProjectsFolderPath, _selectedProject);
            string newPath = Path.Combine(ProjectsFolderPath, newName);

            try
            {
                if (Directory.Exists(newPath))
                {
                    MessageBox.Show("A project with this name already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ProjectNameBox.Text = _selectedProject;
                    return;
                }

                SaveDirtyDescription();
                Directory.Move(oldPath, newPath);
                _selectedProject = newName;
                RefreshProjectList();
                LoadProjectData(newName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to rename project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ProjectNameBox.Text = _selectedProject;
            }
        }

        private void OnNewProjectClick(object sender, RoutedEventArgs e)
        {
            string baseName = "New Project";
            string newName = baseName;
            int counter = 1;

            while (Directory.Exists(Path.Combine(ProjectsFolderPath, newName)))
            {
                newName = $"{baseName} ({counter})";
                counter++;
            }

            string newPath = Path.Combine(ProjectsFolderPath, newName);
            Directory.CreateDirectory(newPath);
            File.WriteAllText(Path.Combine(newPath, "description.txt"), "A new level project.");
            File.WriteAllText(Path.Combine(newPath, "level.json"),
                JsonSerializer.Serialize(new LevelDocument { Name = newName }, new JsonSerializerOptions { WriteIndented = true }));

            ProjectSearchBox.Clear();
            RefreshProjectList();
            ProjectListBox.SelectedItem = newName;
        }

        private void OnDeleteProjectClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedProject)) return;

            if (_selectedProject == UndeletableProject)
            {
                MessageBox.Show($"The project '{UndeletableProject}' cannot be deleted.", "Action Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Are you sure you want to delete '{_selectedProject}'? This cannot be undone.", "Delete Project", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                string targetDir = Path.Combine(ProjectsFolderPath, _selectedProject);
                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }

                _selectedProject = string.Empty;
                RefreshProjectList();
            }
        }

        private void OnDuplicateProjectClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedProject)) return;

            string originalPath = Path.Combine(ProjectsFolderPath, _selectedProject);
            string newName = $"{_selectedProject} (Copy)";
            int counter = 1;

            while (Directory.Exists(Path.Combine(ProjectsFolderPath, newName)))
            {
                newName = $"{_selectedProject} (Copy {counter})";
                counter++;
            }

            string newPath = Path.Combine(ProjectsFolderPath, newName);
            CopyDirectory(originalPath, newPath);
            var copiedLevelPath = Path.Combine(newPath, "level.json");
            if (File.Exists(copiedLevelPath))
            {
                try
                {
                    var copiedLevel = JsonSerializer.Deserialize<LevelDocument>(File.ReadAllText(copiedLevelPath));
                    if (copiedLevel is not null)
                    {
                        copiedLevel.Name = newName;
                        File.WriteAllText(copiedLevelPath, JsonSerializer.Serialize(copiedLevel, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
                catch { }
            }

            ProjectSearchBox.Clear();
            RefreshProjectList();
            ProjectListBox.SelectedItem = newName;
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            }
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(subDir, Path.Combine(targetDir, Path.GetFileName(subDir)));
            }
        }

        private void OnThumbnailClick(object sender, MouseButtonEventArgs e)
        {
            if (_selectedProject == UndeletableProject)
            {
                MessageBox.Show("You cannot change this project's thumbnail.", "Action Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                string projPath = Path.Combine(ProjectsFolderPath, _selectedProject);
                foreach (string ext in new[] { ".png", ".jpg", ".jpeg" })
                {
                    string oldThumb = Path.Combine(projPath, $"thumbnail{ext}");
                    if (File.Exists(oldThumb)) File.Delete(oldThumb);
                }

                string newExt = Path.GetExtension(dlg.FileName);
                string destPath = Path.Combine(projPath, $"thumbnail{newExt}");
                File.Copy(dlg.FileName, destPath, true);

                LoadProjectData(_selectedProject);
            }
        }

        private void OnEditProjectClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedProject)) return;
            SaveDirtyDescription();
            var levelPath = Path.Combine(ProjectsFolderPath, _selectedProject, "level.json");
            try
            {
                if (!File.Exists(levelPath))
                    File.WriteAllText(levelPath, JsonSerializer.Serialize(
                        new LevelDocument { Name = _selectedProject },
                        new JsonSerializerOptions { WriteIndented = true }));

                new MainWindow(levelPath).Show();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Couldn't open this project in the level editor.\n\n{ex.Message}", "Open project", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenFolderClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedProject)) return;
            var path = Path.Combine(ProjectsFolderPath, _selectedProject);
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Couldn't open the project folder.\n\n{ex.Message}", "Open folder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnPublishClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedProject)) return;
            SaveDirtyDescription();
            var projectPath = Path.Combine(ProjectsFolderPath, _selectedProject);
            var dialog = new SaveFileDialog
            {
                Title = "Export project package",
                Filter = "ZIP project package (*.zip)|*.zip",
                DefaultExt = ".zip",
                AddExtension = true,
                FileName = _selectedProject + ".zip"
            };
            if (dialog.ShowDialog(this) != true) return;
            try
            {
                var outputPath = Path.GetFullPath(dialog.FileName);
                var folderPrefix = Path.GetFullPath(projectPath) + Path.DirectorySeparatorChar;
                if (outputPath.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Choose a ZIP location outside the project folder.");

                if (File.Exists(outputPath)) File.Delete(outputPath);
                ZipFile.CreateFromDirectory(projectPath, outputPath, CompressionLevel.Optimal, true);
                MessageBox.Show(this, $"Project package exported to:\n{outputPath}", "Export project", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Couldn't export the project package.\n\n{ex.Message}", "Export project", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}