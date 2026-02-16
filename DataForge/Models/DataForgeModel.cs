using Elder.DataForge.Core.CodeGenerator;
using Elder.DataForge.Core.ContentExtracter.Excel;
using Elder.DataForge.Core.DataExporter.MessagePack;
using Elder.DataForge.Core.DllBuilder;
using Elder.DataForge.Core.DocumentReader.Excel;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Core.SchemaAnalyzer.Excel;
using Elder.DataForge.Models.Data;
using Elder.Reactives.Helpers;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Elder.DataForge.Models
{
    internal class DataForgeModel : IModel
    {
        private readonly IDocumentReader _documentReader;
        private readonly ITableSchemaAnalyzer _schemaAnalyzer;
        private readonly IDocumentContentExtracter _contentExtracter;
        private readonly ISourceCodeGenerator _codeGenerator;
        private readonly IDataExporter _dataExporter;
        private readonly IDllBuilder _dllBuilder;

        private CompositeDisposable _disposables = new();

        private bool _tasking = false;

        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();

        public ObservableCollection<DocumentInfoData> DocumenttInfoDataCollection => _documentReader.DocumenttInfoDataCollection;
        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        
        public DataForgeModel()
        {
            _documentReader = new ExcelDocumentReader();
            _schemaAnalyzer = new TableSchemaAnalyzer();
            _contentExtracter = new ExcelContentExtracter();
            _codeGenerator = new SourceCodeGenerator(_contentExtracter, _schemaAnalyzer);
            _dataExporter = new ExcelToMessagePackData(_contentExtracter, _schemaAnalyzer);
            _dllBuilder = new DllBuilder();

            SubscribeToIProgressNotifiers(_codeGenerator, _dataExporter, _dllBuilder);
        }

        private void OnSourceProgressLevelUpdated(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void OnSourceProgressValueUpdated(float progressValue) => _updateProgressValue.OnNext(progressValue);

        private void SubscribeToIProgressNotifiers(params IProgressNotifier[] notifiers)
        {
            foreach (var notifier in notifiers)
            {
                notifier.OnProgressLevelUpdated.Subscribe(OnSourceProgressLevelUpdated).Add(_disposables);
                notifier.OnProgressValueUpdated.Subscribe(OnSourceProgressValueUpdated).Add(_disposables);
            }
        }

        private void RunTask(Func<Task> taskFunc)
        {
            if (_tasking)
                return;

            _tasking = true;
            Task.Run(async () => await RunTaskAsync(taskFunc));
        }

        private async Task RunTaskAsync(Func<Task> taskFunc)
        {
            try 
            {
                await taskFunc.Invoke();
            }
            finally 
            { 
                _tasking = false; 
            }
        }

        public void LoadDocument()
        {
            RunTask(_documentReader.ReadDocumentProcessAsync);
        }

        public void GenerateSourceCodes()
        {
            RunTask(GenerateSourceCodesAsync);
        }

        private async Task<bool> GenerateSourceCodesAsync()
        {
            var result = await _codeGenerator.GenerateSourceCodeAsync(DocumenttInfoDataCollection);
            return result;
        }

        public void ExportData()
        {
            RunTask(ExportDataAsync);
        }

        private async Task<bool> ExportDataAsync()
        {
            var result = await _dataExporter.ExportDataAsync(DocumenttInfoDataCollection);
            return result;
        }

        public void BuildDll()
        {
            RunTask(_dllBuilder.BuildDllAsync);
        }
    }
}