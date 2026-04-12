using Elder.DataForge.Core.CodeGenerator;
using Elder.DataForge.Core.ContentExtracter.Excel;
using Elder.DataForge.Core.DataExporter.MessagePack;
using Elder.DataForge.Core.DllBuilder;
using Elder.DataForge.Core.DocumentReader.Excel;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Core.SchemaAnalyzer.Excel;
using Elder.DataForge.Models.Data;
using Elder.Reactives.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Elder.DataForge.Models
{
    internal class DataForgeModel : IModel, IProgressNotifier
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
        private Subject<string> _updateOutputLog = new();

        public ObservableCollection<DocumentInfoData> DocumenttInfoDataCollection => _documentReader.DocumenttInfoDataCollection;
        public ObservableCollection<string> LogMessages { get; } = new(); // ✨ Output Log 데이터 통 추가
        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        public IObservable<string> OnOutputLogUpdated => _updateOutputLog;

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
        private void OnSourceOutputLogUpdated(string outputLog) => _updateOutputLog.OnNext(outputLog);

        private void SubscribeToIProgressNotifiers(params IProgressNotifier[] notifiers)
        {
            foreach (var notifier in notifiers)
            {
                notifier.OnProgressLevelUpdated.Subscribe(OnSourceProgressLevelUpdated).Add(_disposables);
                notifier.OnProgressValueUpdated.Subscribe(OnSourceProgressValueUpdated).Add(_disposables);

                // ✨ 핵심 해결책: UI 스레드(Dispatcher)에 태워서 에러 없이 로그를 추가합니다.
                notifier.OnOutputLogUpdated.Subscribe(msg =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogMessages.Add(msg);
                        OnSourceOutputLogUpdated(msg); // 뷰에서 자동 스크롤을 트리거하기 위해 유지
                    });
                }).Add(_disposables);
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

        public void OpenFolder()
        {
            try
            {
                // Settings에서 유저가 설정한 Output 경로를 가져옵니다.
                string outputPath = Properties.Settings.Default.OutputPath;

                if (string.IsNullOrEmpty(outputPath))
                {
                    _updateProgressLevel.OnNext("Open Folder Failed: Output Path is not configured.");
                    return;
                }

                if (!Directory.Exists(outputPath))
                {
                    _updateProgressLevel.OnNext("Open Folder Failed: Directory does not exist. Please create/export data first.");
                    return;
                }

                // 윈도우 탐색기(explorer.exe)를 실행하여 해당 경로를 엽니다.
                var startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{outputPath}\"",
                    UseShellExecute = true
                };

                Process.Start(startInfo);

                _updateProgressLevel.OnNext($"Opened folder: {outputPath}");
            }
            catch (Exception ex)
            {
                _updateProgressLevel.OnNext($"Open Folder Error: {ex.Message}");
            }
        }
    }
}