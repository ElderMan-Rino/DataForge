using Elder.DataForge.Core.CodeGenerator.MessagePack;
using Elder.DataForge.Core.CodeSaver;
using Elder.DataForge.Core.Commons.Enum;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Core.PostProcessor.MessagePack;
using Elder.DataForge.Core.Registry;
using Elder.DataForge.Models.Data;
using Elder.Reactives.Helpers;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;


namespace Elder.DataForge.Core.CodeGenerator
{
    public class SourceCodeGenerator : ISourceCodeGenerator
    {
        private readonly SheetRegistryManager _registryManager = new();

        private CompositeDisposable _disposables = new();

        private IDocumentContentExtracter _contentExtracter;
        private ITableSchemaAnalyzer _schemaAnalyzer;
        private ISourceCodeSaver _codeSaver;
        private ICodeEmitter _codeEmitter;
        private MessagePackPostProcessor _postProcessor;

        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();
        private Subject<string> _updateOutputLog = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        public IObservable<string> OnOutputLogUpdated => _updateOutputLog;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);
        private void UpdateOutputLog(string outputLog) => _updateOutputLog.OnNext(outputLog);
        private void OnSourceProgressLevelUpdated(string progressLevel) => UpdateProgressLevel(progressLevel);
        private void OnSourceProgressValueUpdated(float progressValue) => UpdateProgressValue(progressValue);
        private void OnSourceOutputLogUpdated(string outputLog) => UpdateOutputLog(outputLog);

        public SourceCodeGenerator(IDocumentContentExtracter contentExtracter, ITableSchemaAnalyzer schemaAnalyzer)
        {
            _contentExtracter = contentExtracter;
            _schemaAnalyzer = schemaAnalyzer;
            _codeEmitter = new MessagePackSourceEmitter();
            _codeSaver = new FileSourceCodeSaver();
            _postProcessor = new MessagePackPostProcessor();

            SubscribeToIProgressNotifiers(_codeEmitter, _codeSaver, _contentExtracter, _postProcessor);
        }

        private void SubscribeToIProgressNotifiers(params IProgressNotifier[] notifiers)
        {
            foreach (var notifier in notifiers)
            {
                notifier.OnProgressLevelUpdated.Subscribe(OnSourceProgressLevelUpdated).Add(_disposables);
                notifier.OnProgressValueUpdated.Subscribe(OnSourceProgressValueUpdated).Add(_disposables);
                notifier.OnOutputLogUpdated.Subscribe(OnSourceOutputLogUpdated).Add(_disposables);
            }
        }


        public async Task<bool> GenerateSourceCodeAsync(IReadOnlyList<DocumentInfoData> documentInfos)
        {
            UpdateProgressLevel("GenerateSourceCode Start");

            var documentContents = await ExtractContentAsync(documentInfos);
            if (documentContents == null || !documentContents.Any())
            {
                UpdateProgressLevel("GenerateSourceCode ExtractContentAsync Failed");
                return false;
            }

            var domainSchemas = ParseDomainSchemas(documentContents);
            if (domainSchemas == null || !domainSchemas.Any())
            {
                UpdateProgressLevel("GenerateSourceCode.ParseDomainSchemas Failed");
                return false;
            }

            // 레지스트리 병합 및 저장
            UpdateProgressLevel("Updating Sheet Registry...");
            var registry = _registryManager.Load();
            var newTableNames = domainSchemas.Select(s => s.TableName);
            registry = _registryManager.Merge(registry, newTableNames);
            _registryManager.Save(registry);

            var generateSourceCodes = await GenerateSourceCodesAsync(domainSchemas);
            if (generateSourceCodes == null || !generateSourceCodes.Any())
            {
                UpdateProgressLevel("GenerateSourceCode.GenerateSourceCodesAsync Failed");
                return false;
            }

            // GeneratedBlobLoader는 전체 레지스트리 기준으로 생성
            var activeSheets = _registryManager.GetSheets(); 
            generateSourceCodes.Add(GenerateBlobLoader(activeSheets));

            var isSaveSuccess = await SaveGeneratedSourcesAsync(generateSourceCodes);
            if (!isSaveSuccess)
            {
                UpdateProgressLevel("GenerateSourceCode.SaveGeneratedSourcesAsync Failed");
                return false;
            }

            await RunPostProcessingServiceAsync();
            return true;
        }

        private GeneratedSourceCode GenerateBlobLoader(List<SheetEntry> activeSheets)
        {
            // TableSchema 대신 SheetEntry 리스트를 받는 오버로드 추가
            return new GeneratedSourceCode(
                "GeneratedBlobLoader.cs",
                _codeEmitter.GenerateDataLoaderContent(activeSheets),
                SourceCategory.UnityScripts
            );
        }

        private async Task<Dictionary<string, DocumentContentData>> ExtractContentAsync(IReadOnlyList<DocumentInfoData> documentInfos)
        {
            return await _contentExtracter.ExtractDocumentContentDataAsync(documentInfos);
        }

        private List<TableSchema> ParseDomainSchemas(Dictionary<string, DocumentContentData> documentContents)
        {
            return _schemaAnalyzer.AnalyzeFields(documentContents);
        }

        private async Task<List<GeneratedSourceCode>> GenerateSourceCodesAsync(List<TableSchema> schemas)
        {
            return await _codeEmitter.GenerateAsync(schemas);
        }

        private async Task<bool> SaveGeneratedSourcesAsync(List<GeneratedSourceCode> sourceCodes)
        {
            return await _codeSaver.ExportAsync(sourceCodes);
        }

        private async Task RunPostProcessingServiceAsync()
        {
            await _postProcessor.PostProcessAsync();
        }
    }
}
