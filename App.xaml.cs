using Elder.DataForge;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models;
using Elder.DataForge.ViewModels;
using Elder.DataForge.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Elder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IModel? _model;
        private IViewModel? _viewModel;
        private Window? _view;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var status = InitializeApp();
            if (!string.IsNullOrEmpty(status))
            {
                MessageBox.Show(status); // ShowMessageBox 대신 기본 메서드 사용 예시
                Shutdown(); // 초기화 실패 시 앱 종료
                return;
            }

            // 모든 연결이 끝난 후 화면을 띄웁니다.
            _view?.Show();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            var status = InitializeApp();
            if (!string.IsNullOrEmpty(status))
            {
                ShowMessageBox(status);
                return;
            }
        }

        private void ShowMessageBox(string message)
        {
            // 어떤 메세지박스를 보여주는지 
        }

        private string InitializeApp()
        {
            if (!InitializeModel())
                return "InitializeModel Failed";

            if (!InitializeViewModel())
                return "InitializeViewModel Failed";

            if (TryBindViewModelToModel() == false)
                return "BindViewModelToModel Failed";

            if (!InitializeView())
                return "InitializeView Failed";

            return string.Empty;
        }

        private bool InitializeView()
        {
            _view = new MainWindow
            {
                DataContext = _viewModel 
            };
            return _view != null;
        }

        private bool InitializeViewModel()
        {
            _viewModel = CreateViewModel();
            return _viewModel != null;
        }

        private bool InitializeModel()
        {
            _model = CreateModel();
            return _model != null;
        }

        private IViewModel CreateViewModel()
        {
            return new DataForgeViewModel();
        }

        private IModel CreateModel()
        {
            return new DataForgeModel();
        }

        private bool? TryBindViewModelToModel()
        {
            return _viewModel?.TryBindModel(_model);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _viewModel?.Dispose();
            _model?.Dispose();
            base.OnExit(e);
        }
    }

}
