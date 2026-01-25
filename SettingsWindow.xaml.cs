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

namespace DataForge
{
    /// <summary>
    /// SettingsWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }
        // 'Browse' 버튼 클릭 시 폴더 선택 창 띄우기
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                // XAML의 TextBox 이름이 TxtProjectPath라고 가정
                if (this.FindName("TxtProjectPath") is System.Windows.Controls.TextBox textBox)
                {
                    textBox.Text = dialog.FolderName;
                }
            }
        }

        // 'Save' 버튼 클릭 시 (현재는 창만 닫음, 필요시 모델에 데이터 전달)
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        // 'Cancel' 버튼 클릭 시
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
