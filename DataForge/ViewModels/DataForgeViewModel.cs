using CommunityToolkit.Mvvm.Input;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Views;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Input;

namespace Elder.DataForge.ViewModels
{
    public class DataForgeViewModel : IViewModel
    {
        private IModel? _model;

        public ObservableCollection<DocumentInfoData>? DocumenttInfoDataCollection => _model?.DocumenttInfoDataCollection;

        public ICommand LoadDocumentsCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand GenerateSourceCodesCommand { get; }
        public ICommand BuildDLLCommand { get; }
        public ICommand OpenSettingCommand { get; }

        public IObservable<string> OnProgressLevelUpdated => _model?.OnProgressLevelUpdated;
        public IObservable<float> OnProgressValueUpdated => _model?.OnProgressValueUpdated;

        public DataForgeViewModel()
        {
            LoadDocumentsCommand = new RelayCommand(OnLoadDocumentsCommand);
            ExportDataCommand = new RelayCommand(OnExportDataCommand);
            GenerateSourceCodesCommand = new RelayCommand(OnGenerateSourceCodesCommand);
            BuildDLLCommand = new RelayCommand(OnBuildDLLCommand);
            OpenSettingCommand = new RelayCommand(OnOpenSettingCommand);
        }

        private void OnLoadDocumentsCommand()
        {
            _model?.LoadDocument();
        }

        private void OnExportDataCommand()
        {
            _model?.ExportData();
        }

        private void OnGenerateSourceCodesCommand()
        {
            _model?.GenerateSourceCodes();
        }

        private void OnBuildDLLCommand()
        {
            //_model?.BuildDlls();
        }

        private void OnOpenSettingCommand()
        {
            var settingsWin = OpenSettingWindow();
            settingsWin.ShowDialog();
        }

        private SettingsWindow OpenSettingWindow()
        {
            var settingsWin = new SettingsWindow();
            settingsWin.Owner = Application.Current.MainWindow;
            return settingsWin;
        }

        public void FinalizeBinding()
        {

        }

        public bool TryBindModel(IModel model)
        {
            _model = model as DataForgeModel;
            return _model != null;
        }
    }
}
