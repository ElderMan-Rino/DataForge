using Elder.DataForge.Core.CodeGenerators.MessagePack;
using Elder.DataForge.Core.CodeSaver;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Core.PostProcessor.MessagePack;
using Elder.DataForge.Models.Data;
using Elder.Reactives.Helpers;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;


namespace Elder.DataForge.Core.CodeGenerators
{
    public class SourceCodeGenerator : ISourceCodeGenerator
    {
        private CompositeDisposable _disposables = new();

        private IDocumentContentExtracter _contentExtracter;
        private ITableSchemaAnalyzer _schemaAnalyzer;
        private ISourceCodeSaver _codeSaver;
        private ICodeEmitter _codeEmitter;
        private MessagePackPostProcessor _postProcessor;

        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);
        private void OnSourceProgressLevelUpdated(string progressLevel) => UpdateProgressLevel(progressLevel);
        private void OnSourceProgressValueUpdated(float progressValue) => UpdateProgressValue(progressValue);

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

            var generateSourceCodes = await GenerateSourceCodesAsync(domainSchemas);
            if (generateSourceCodes == null || !generateSourceCodes.Any())
            {
                UpdateProgressLevel("GenerateSourceCode.GenerateSourceCodesAsync Failed");
                return false;
            }

            var isSaveSuccess = await SaveGeneratedSourcesAsync(generateSourceCodes);
            if (!isSaveSuccess)
            {
                UpdateProgressLevel("GenerateSourceCode.SaveGeneratedSourcesAsync Failed");
                return false;
            }

            await RunPostProcessingServiceAsync();
            
            return true;
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
