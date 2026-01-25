using CommunityToolkit.Mvvm.Input;
using DataForge;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models;
using Elder.DataForge.Models.Data;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Input;

namespace Elder.DataForge.ViewModels
{
    public class DataForgeViewModel : IViewModel
    {
        private readonly CompositeDisposable _disposables = new();
        
        private DataForgeModel? _model;

        public ObservableCollection<DocumentInfoData>? DocumenttInfoDataCollection => _model?.DocumenttInfoDataCollection;

        public ICommand LoadDocumentsCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand CreateElementsCommand { get; }
        public ICommand ExportDLLCommand { get; }
        public ICommand OpenSettingCommand { get; }

        public DataForgeViewModel()
        {
            LoadDocumentsCommand = new RelayCommand(OnLoadDocumentsCommand);
            ExportDataCommand = new RelayCommand(OnExportDataCommand);
            CreateElementsCommand = new RelayCommand(OnCreateElementsCommand);
            ExportDLLCommand = new RelayCommand(OnExportDLLCommand);
            OpenSettingCommand = new RelayCommand(OnOpenSettingCommand);
        }

        private void OnLoadDocumentsCommand()
        {
            _model?.HandleLoadDocument();
        }

        private void OnExportDataCommand()
        {
            //_model?.ExportData();
        }

        private void OnCreateElementsCommand()
        {
            _model?.CreateElements();
        }

        private void OnExportDLLCommand()
        {

        }

        private void OnOpenSettingCommand()
        {
            // SettingsWindow 인스턴스 생성
            SettingsWindow settingsWin = new SettingsWindow();

            // 메인 윈도우를 소유자로 설정 (중앙 정렬을 위함)
            settingsWin.Owner = Application.Current.MainWindow;

            // 대화 상자 형식으로 표시
            if (settingsWin.ShowDialog() == true)
            {
                // 사용자가 'Save'를 눌렀을 때의 후속 처리
            }
        }

        public void FinalizeBinding()
        {
          
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public bool TryBindModel(IModel model)
        {
            _model = model as DataForgeModel;
            return _model != null;
        }
    }
}
