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
            var viewModel = (DataContext as IViewModel); // IViewModel에 OnOutputLogUpdated가 있다고 가정합니다.
            if (viewModel == null)
                return;

            viewModel.OnProgressLevelUpdated
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(HandleOnProgressLevelUpdated);

            viewModel.OnProgressValueUpdated
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(HandleProgressValueUpdated);

            // ✨ 로그가 들어올 때마다 ListBox를 아래로 스크롤하도록 구독 추가
            if (viewModel is DataForgeViewModel dfViewModel)
            {
                dfViewModel.OnOutputLogUpdated
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(_ => ScrollDownInfoView());
            }
        }

        private void HandleOnProgressLevelUpdated(string level)
        {
            ProgressLevelText.Text = level;
            ScrollDownInfoView();
        }

        private void ScrollDownInfoView()
        {
            if (LogListBox.Items.Count <= 0)
            {
                return;
            }
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