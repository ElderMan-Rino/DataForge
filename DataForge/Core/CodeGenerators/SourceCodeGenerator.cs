using Elder.DataForge.Core.CodeGenerators.MessagePack;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace Elder.DataForge.Core.CodeGenerators
{
    public class SourceCodeGenerator : ISourceCodeGenerator
    {
        private IDocumentContentExtracter _contentExtracter;
        private ITableSchemaAnalyzer _schemaAnalyzer;
        private ICodeTemplateEngine _templateEngine;

        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();



        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        public SourceCodeGenerator(IDocumentContentExtracter contentExtracter, ITableSchemaAnalyzer schemaAnalyzer)
        {
            _contentExtracter = contentExtracter;
            _schemaAnalyzer = schemaAnalyzer;
            _templateEngine = new MessagePackTemplateEngine();
        }
        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);

        public async Task<bool> GenerateSourceCodeAsync(IReadOnlyList<DocumentInfoData> documentInfos)
        {
            UpdateProgressLevel("GenerateSourceCode");

            // 데이터 콘텐츠 추출
            var result = await ExtractContentAsync(documentInfos);
            if (!result)
                return false;

            // 그냥 데이터 콘텐츠 추출을 매번 하자
            
            // 여기서 소스코드 string 생성

            // 여기서 string을 받아서 소스 코드 생성

            // mpc 있는지 확인? -> 이건 나중에 post로 처리

            return true;
        }

        private async Task<bool> ExtractContentAsync(IReadOnlyList<DocumentInfoData> documentInfos)
        {
            UpdateProgressLevel("ExtractContent");

            UpdateProgressLevel("ExtractContent Failed");
            return true;
        }
    }
}
