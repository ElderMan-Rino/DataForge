using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models;
using Elder.DataForge.Models.Data;
using Elder.Reactives.Helpers;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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

        public DataForgeViewModel()
        {
            LoadDocumentsCommand = new RelayCommand(OnLoadDocumentsCommand);
            ExportDataCommand = new RelayCommand(OnExportDataCommand);
            CreateElementsCommand = new RelayCommand(OnCreateElementsCommand);
            ExportDLLCommand = new RelayCommand(OnExportDLLCommand);
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
