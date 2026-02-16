using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.ViewModels;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Elder.DataForge.Views
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

        public void InitializeView()
        {
            SetupProgressSubscription();
        }

        private void SetupProgressSubscription()
        {
            var viewModel = (DataContext as IViewModel);
            if (viewModel == null) 
                return;

            viewModel.OnProgressLevelUpdated
                .ObserveOn(SynchronizationContext.Current) // UI 스레드에서 실행 보장
                .Subscribe(HandleOnProgressLevelUpdated);

            // 2. Progress Value (수치) 업데이트
            viewModel.OnProgressValueUpdated
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(HandleProgressValueUpdated);
        }

        private void HandleOnProgressLevelUpdated(string level)
        {
            ProgressLevelText.Text = level;
            ScrollDownInfoView();
        }
        private void ScrollDownInfoView()
        {
            LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
        }

        private void HandleProgressValueUpdated(float value)
        {
            ExportProgressBar.Value = value;
            ExportProgress.Content = $"{value:F0}%";
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