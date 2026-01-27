using Elder.DataForge.Core;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

        // 기본 출력 경로 선택 (Tools/Output)
        private void BtnBaseBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                TxtBaseOutputPath.Text = dialog.FolderName;
            }
        }

      
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {

            Elder.DataForge.Properties.Settings.Default.BaseOutputPath = TxtBaseOutputPath.Text;
            Elder.DataForge.Properties.Settings.Default.RootNamespace = TxtRootNamespace.Text;

            Elder.DataForge.Properties.Settings.Default.Save();

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
