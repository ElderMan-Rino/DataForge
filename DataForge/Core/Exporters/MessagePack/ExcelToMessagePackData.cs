using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.Reactives.Helpers;
using MessagePack;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Elder.DataForge.Core.Exporters.MessagePack
{
    public class ExcelToMessagePackData : IDataExporter
    {
        private CompositeDisposable _disposables = new();

        private readonly IDocumentContentExtracter _contentExtracter;
        private readonly ITableSchemaAnalyzer _schemaAnalyzer;

        private readonly Subject<string> _updateProgressLevel = new();
        private readonly Subject<float> _updateProgressValue = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);
        private void OnSourceProgressLevelUpdated(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void OnSourceProgressValueUpdated(float progressValue) => _updateProgressValue.OnNext(progressValue);

        public ExcelToMessagePackData(IDocumentContentExtracter contentExtracter, ITableSchemaAnalyzer schemaAnalyzer)
        {
            _contentExtracter = contentExtracter;
            _schemaAnalyzer = schemaAnalyzer;

            SubscribeToIProgressNotifiers(_contentExtracter);
        }

        private void SubscribeToIProgressNotifiers(params IProgressNotifier[] notifiers)
        {
            foreach (var notifier in notifiers)
            {
                notifier.OnProgressLevelUpdated.Subscribe(OnSourceProgressLevelUpdated).Add(_disposables);
                notifier.OnProgressValueUpdated.Subscribe(OnSourceProgressValueUpdated).Add(_disposables);
            }
        }

        public async Task<bool> ExportDataAsync(IReadOnlyList<DocumentInfoData> documentInfos)
        {
            try
            {
                UpdateProgressLevel("Starting Data Export with Schema Analysis...");

                // 1. 최신 컨텐츠 추출 및 스키마 분석 수행
                // 이 과정을 통해 필드가 Size별로 재정렬된 최신 TableSchema를 얻습니다.
                var contents = await _contentExtracter.ExtractDocumentContentDataAsync(documentInfos);
                var schemas = _schemaAnalyzer.AnalyzeFields(contents);

                if (schemas == null || !schemas.Any())
                {
                    UpdateProgressLevel("No data found to export.");
                    return false;
                }

                // 저장 경로 설정 (Settings 등에서 가져오도록 확장 가능)
                string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportedData");
                if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

                for (int i = 0; i < schemas.Count; i++)
                {
                    var schema = schemas[i];
                    UpdateProgressLevel($"Syncing & Exporting Table: {schema.TableName} ({i + 1}/{schemas.Count})");

                    // 2. 분석된 스키마의 KeyIndex(정렬 순서)에 맞춰 데이터 재구성
                    var serializedTable = new List<Dictionary<int, object>>();

                    foreach (var row in schema.RawRows)
                    {
                        var rowMap = new Dictionary<int, object>();

                        foreach (var field in schema.AnalyzedFields)
                        {
                            // 분석기(Analyzer)가 정의한 타입과 인덱스 정보를 그대로 활용
                            object parsedValue = ParseValueBySchema(field, row);

                            // 중요: 분석기가 재정렬하여 할당한 KeyIndex를 MessagePack의 키로 사용
                            // 이를 통해 유니티의 DOD 구조체 레이아웃과 바이너리 순서가 1:1로 일치하게 됨
                            rowMap.Add(field.KeyIndex, parsedValue);
                        }
                        serializedTable.Add(rowMap);
                    }

                    // 3. MessagePack 직렬화 및 파일 저장
                    byte[] bin = MessagePackSerializer.Serialize(serializedTable);
                    await File.WriteAllBytesAsync(Path.Combine(outputPath, $"{schema.TableName}.bytes"), bin);

                    UpdateProgressValue((float)(i + 1) / schemas.Count * 100f);
                }

                UpdateProgressLevel("Data Export Completed Successfully.");
                return true;
            }
            catch (Exception ex)
            {
                UpdateProgressLevel($"Export Error: {ex.Message}");
                return false;
            }
        }

        private object ParseValueBySchema(AnalyzedField field, List<string> row)
        {
            if (field.IsList)
            {
                var list = new List<object>();
                foreach (var idx in field.ExcelIndices)
                {
                    if (row.Count > idx && !string.IsNullOrEmpty(row[idx]))
                        list.Add(ConvertToPrimitive(field.ManagedType.Replace("List<", "").Replace(">", ""), row[idx]));
                }
                return list;
            }

            int targetIdx = field.ExcelIndices[0];
            if (row.Count <= targetIdx || string.IsNullOrEmpty(row[targetIdx])) return null;

            return ConvertToPrimitive(field.ManagedType, row[targetIdx]);
        }

        private object ConvertToPrimitive(string type, string value)
        {
            return type.ToLower() switch
            {
                "int" or "int32" => int.Parse(value),
                "float" or "single" => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
                "long" or "int64" => long.Parse(value),
                "double" => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
                "bool" or "boolean" => bool.Parse(value),
                "string" => value,
                _ => value
            };
        }
    }
}