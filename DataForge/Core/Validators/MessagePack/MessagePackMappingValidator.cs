using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using MessagePack;
using System.Reactive.Subjects;

namespace Elder.DataForge.Core.Validators.MessagePack
{
    internal class MessagePackMappingValidator : IExportValidator
    {
        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();
        private Subject<string> _updateOutputLog = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        public IObservable<string> OnOutputLogUpdated => _updateOutputLog;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);
        private void UpdateOutputLog(string outputLog) => _updateOutputLog.OnNext(outputLog);

        public async Task<bool> ValidateAsync(byte[] data, TableSchema schema)
        {
            try
            {
                // 🚨 핵심 수정: Dictionary가 아닌 object[] 형태로 역직렬화하여 검증합니다.
                var deserialized = MessagePackSerializer.Deserialize<List<object[]>>(data);

                if (deserialized.Count == 0) return true;

                // 첫 번째 행을 샘플로 스키마 일치 여부 확인
                var firstRow = deserialized[0];
                if (firstRow.Length != schema.AnalyzedFields.Count)
                {
                    UpdateOutputLog($"[Validation Error] 필드 개수 불일치: {schema.TableName}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                UpdateOutputLog($"[Validation Exception] {ex.Message}");
                return false;
            }
        }
    }
}
