using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using MessagePack;
using System.Reactive.Subjects;

namespace Elder.DataForge.Core.Validators.MessagePack
{
    internal class MessagePackMappingValidator : IExportValidator
    {
        private readonly Subject<string> _updateProgressLevel = new();
        private readonly Subject<float> _updateProgressValue = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);

        public async Task<bool> ValidateAsync(byte[] binaryData, TableSchema schema)
        {
            try
            {
                UpdateProgressLevel($"Validating Schema Mapping for: {schema.TableName}");

                // 1. 역직렬화 테스트 (Dictionary 형태로 구조적 정합성 확인)
                var testData = MessagePackSerializer.Deserialize<List<Dictionary<int, object>>>(binaryData);

                if (testData == null || testData.Count == 0)
                {
                    UpdateProgressLevel("Warning: Exported data is empty.");
                    return true;
                }

                // 2. 필드 존재 여부 및 타입 샘플링 검사
                var sampleRow = testData[0];
                foreach (var field in schema.AnalyzedFields)
                {
                    if (!sampleRow.ContainsKey(field.KeyIndex))
                    {
                        UpdateProgressLevel($"Error: KeyIndex {field.KeyIndex} ({field.Name}) missing in binary.");
                        return false;
                    }
                }

                UpdateProgressLevel($"Validation Success: {schema.TableName}");
                return true;
            }
            catch (Exception ex)
            {
                UpdateProgressLevel($"Validation Exception: {ex.Message}");
                return false;
            }
        }
    }
}
