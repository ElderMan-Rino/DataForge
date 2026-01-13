using Elder.DataForge.Core.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace Elder.DataForge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IView
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnListViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustColumnWidthToFill();
        }

        private void AdjustColumnWidthToFill()
        {
            if (TableInfoListView.View is not GridView gridView)
                return;

            if (gridView.Columns.Count < 2)
                return;

            var fixedWidth = gridView.Columns[0].Width;
            var workingWidth = TableInfoListView.ActualWidth - fixedWidth;
            gridView.Columns[1].Width = workingWidth;
        }

        public void Dispose()
        {

        }
    }
}