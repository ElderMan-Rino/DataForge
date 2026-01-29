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
            InitializeSettingWindow();
        }

        private void InitializeSettingWindow()
        {
            TxtBaseOutputPath.Text = Properties.Settings.Default.BaseOutputPath;
            TxtRootNamespace.Text = Properties.Settings.Default.RootNamespace;
            TxtUnityDllPath.Text = Properties.Settings.Default.UnityDllPath;
        }

        // 기본 출력 경로 선택 (Tools/Output)
        private void BtnBaseBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
                TxtBaseOutputPath.Text = dialog.FolderName;
        }

        private void BtnUnityPathBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "유니티 엔진 DLL들이 포함된 폴더를 선택해주세요.\n(예: Unity.Entities.dll 등이 있는 PackageCache 하위 폴더)";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    TxtUnityDllPath.Text = dialog.SelectedPath;
            }
        }


        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.BaseOutputPath = TxtBaseOutputPath.Text;
            Properties.Settings.Default.RootNamespace = TxtRootNamespace.Text;
            Properties.Settings.Default.UnityDllPath = TxtUnityDllPath.Text;

            Properties.Settings.Default.Save();

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
