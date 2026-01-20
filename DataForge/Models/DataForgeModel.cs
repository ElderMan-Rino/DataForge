using DataForge.DataForge.Core.Converters.MemoryPack;
using Elder.DataForge.Core.CodeGenerators.MessagePack;
using Elder.DataForge.Core.CodeSaver;
using Elder.DataForge.Core.ContentLoaders.Excels;
using Elder.DataForge.Core.InfoLoaders.Excels;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.Reactives.Helpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Elder.DataForge.Models
{
    internal class DataForgeModel : IModel
    {
        private IDocumentInfoLoader _infoLoader = new ExcelInfoLoader();
        private IDocumentContentExtracter _contentExtracter = new ExcelContentExtracter();
        private ISourceCodeGenerator _codeGenerator = new MessagePackSourceGenerator();
        private ISourceCodeSaver _codeSaver = new FileSourceCodeSaver();

        private string _baseOutputPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Resources\MessagePack"));

        private Dictionary<string, DocumentInfoData> _documenttInfoDataMap = new();
        private Dictionary<string, DocumentContentData> _documentContents = new();

        private CompositeDisposable _disposables = new();

        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();

        private bool _disposed = false;
        private bool _tasking = false;

        public ObservableCollection<DocumentInfoData> DocumenttInfoDataCollection { get; private set; } = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        public DataForgeModel()
        {
            SubscribeToContentLoader();
            SubscribeToDataConverter();
            SubscribeToDataExporter();
        }

        private void SubscribeToContentLoader()
        {
            _contentExtracter.OnProgressLevelUpdated.Subscribe(OnSourceProgressLevelUpdated).Add(_disposables);
            _contentExtracter.OnProgressValueUpdated.Subscribe(OnSourceProgressValueUpdated).Add(_disposables);
        }

        private void SubscribeToDataConverter()
        {
            //_converter.OnProgressLevelUpdated.Subscribe(OnSourceProgressLevelUpdated).Add(_disposables);
            //_converter.OnProgressValueUpdated.Subscribe(OnSourceProgressValueUpdated).Add(_disposables);
        }

        private void SubscribeToDataExporter()
        {
            //_exporter.OnProgressLevelUpdated.Subscribe(OnSourceProgressLevelUpdated).Add(_disposables);
            //_exporter.OnProgressValueUpdated.Subscribe(OnSourceProgressValueUpdated).Add(_disposables);
        }

        private void UpdateProgressLevel(string progressLevel)
        {
            _updateProgressLevel.OnNext(progressLevel);
        }

        private void UpdateProgressValue(float progressValue)
        {
            _updateProgressValue.OnNext(progressValue);
        }

        private void OnSourceProgressLevelUpdated(string progressLevel)
        {
            UpdateProgressLevel(progressLevel);
        }

        private void OnSourceProgressValueUpdated(float progressValue)
        {
            UpdateProgressValue(progressValue);
        }


        public void HandleLoadDocument()
        {
            ClearDocumentInfos();
            ClearDocumentInfoCollection();
            ClearDocumentInfoDataMap();

            LoadDocumentInfos();
        }

        private void LoadDocumentInfos()
        {
            if (!_infoLoader.TryLoadDocumentInfos(out var documentInfoData))
                return;

            if (documentInfoData == null || !documentInfoData.Any())
                return;

            foreach (var documentInfoDatum in documentInfoData)
            {
                AddDocumentInfoToMap(documentInfoDatum);
                AddDocumentInfoToCollection(documentInfoDatum);
            }
        }

        private void AddDocumentInfoToCollection(in DocumentInfoData documentInfoDatum)
        {
            DocumenttInfoDataCollection.Add(documentInfoDatum);
        }

        private void AddDocumentInfoToMap(in DocumentInfoData documentInfoDatum)
        {
            _documenttInfoDataMap.Add(documentInfoDatum.Name, documentInfoDatum);
        }


        public void ExportData()
        {
            //RunTask(ExportDataAsync);
        }

        public void CreateElements()
        {
            RunTask(CreateElementsAsync);
        }

        private async Task<bool> ExportDataAsync()
        {
            var documentContents = await _contentExtracter.ExtractDocumentContentDataAsync(_documenttInfoDataMap.Values);
            if (documentContents == null || !documentContents.Any())
                return false;

            foreach (var documentContent in documentContents)
                _documentContents.Add(documentContent.Key, documentContent.Value);

            return true;
        }

        private async Task<bool> CreateElementsAsync()
        {
            var exportResult = await ExportDataAsync();
            if (!exportResult)
                return false;

            var generatedFiles = await _codeGenerator.GenerateAsync(_documentContents);
            if (generatedFiles == null || !generatedFiles.Any())
                return false;

            string dataPath = Path.Combine(_baseOutputPath, "Data");     // 공용 모델용
            string parserPath = Path.Combine(_baseOutputPath, "Parser"); // 툴 전용 파서용

            // 분류하여 저장할 리스트
            var dataFiles = new List<GeneratedSourceCode>();
            var parserFiles = new List<GeneratedSourceCode>();

            foreach (var file in generatedFiles)
            {
                // 파일명 끝자리를 보고 구분 (.Parser.cs vs .cs)
                if (file.FileName.EndsWith(".Parser.cs"))
                {
                    parserFiles.Add(file);
                }
                else
                {
                    dataFiles.Add(file);
                }
            }

            // 데이터 모델 저장
            var saveDataResult = await _codeSaver.SaveAsync(dataFiles, dataPath);
            if (!saveDataResult)
            {
                return false;
            }
            
            // 파서 저장
            var saveParserResult = await _codeSaver.SaveAsync(parserFiles, parserPath);
            if (!saveParserResult)
            {
                return false;
            }

            string mpcPath = Path.Combine(_baseOutputPath, "Mpc");
            bool mpcResult = await Task.Run(() => RunMPC(dataPath, mpcPath, "Elder.Framework.MessagePack.Generated"));
            if (!mpcResult)
            {
                return false;
            }
            return true;
        }

        private bool RunMPC(string inputPath, string outputPath, string nameSpace)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet", // mpc가 dotnet tool로 설치되어 있어야 합니다.
                    Arguments = $"mpc -i \"{inputPath}\" -o \"{outputPath}\" -n \"{nameSpace}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        // 에러 로그 기록 로직 필요
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
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
            await taskFunc.Invoke();
            _tasking = false;
        }

        private void DisposeDisposableSet()
        {
            _disposables.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // 소멸자 호출 방지
        }

        // Dispose 패턴 구현 (protected virtual)
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ClearDocumentInfoCollection();
                    ClearDocumentInfos();
                    DisposeDisposableSet();
                }
                _disposed = true;
            }
        }

        private void ClearDocumentInfos()
        {
            foreach (var documentInfo in _documenttInfoDataMap.Values)
                documentInfo.Dispose();
            _documenttInfoDataMap.Clear();
        }

        private void ClearDocumentInfoCollection()
        {
            foreach (var documentInfo in DocumenttInfoDataCollection)
                documentInfo.Dispose();
            DocumenttInfoDataCollection.Clear();
        }

        private void ClearDocumentInfoDataMap()
        {
            foreach (var documentContent in _documentContents.Values)
                documentContent.Dispose();
            _documentContents.Clear();
        }

        ~DataForgeModel()
        {
            // 소멸자에서 Dispose 호출
            Dispose(false);
        }
    }
}