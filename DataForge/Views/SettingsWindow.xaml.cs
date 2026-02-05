using Elder.DataForge.Models.Data;
using Microsoft.Win32;
using System.Windows;

namespace Elder.DataForge.Views
{
    /// <summary>
    /// SettingsWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public ForgeSettings Settings { get; private set; } = new();

        public SettingsWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            SettingDefaultValues();
        }

        private void SettingDefaultValues()
        {
            TxtBaseOutputPath.Text = Properties.Settings.Default.OutputPath;
            TxtRootNamespace.Text = Properties.Settings.Default.RootNamespace;
            TxtMsBuildPath.Text = Properties.Settings.Default.MsBuildPath;
        }

        // 기본 출력 경로 선택 (Tools/Output)
        private void BtnBaseBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
                TxtBaseOutputPath.Text = dialog.FolderName;
        }


        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.OutputPath = TxtBaseOutputPath.Text;
            Properties.Settings.Default.RootNamespace = TxtRootNamespace.Text;
            Properties.Settings.Default.MsBuildPath = TxtMsBuildPath.Text;

            Properties.Settings.Default.Save();

            this.DialogResult = true;
            this.Close();
        }


        private void BtnMsBuildBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
            dialog.Title = "Select MSBuild.exe";
            dialog.FileName = "MSBuild.exe";
            if (dialog.ShowDialog() == true)
                TxtMsBuildPath.Text = dialog.FileName;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
