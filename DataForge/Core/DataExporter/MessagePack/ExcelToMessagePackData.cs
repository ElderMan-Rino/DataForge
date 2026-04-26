using Elder.DataForge.Core.Common.Const;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Core.Validators.MessagePack;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excel;
using Elder.Reactives.Helpers;
using MessagePack;
using MessagePack.Resolvers;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Elder.DataForge.Core.DataExporter.MessagePack
{
    public class ExcelToMessagePackData : IDataExporter
    {
        private CompositeDisposable _disposables = new();
        private readonly IExportValidator _validator;

        private readonly IDocumentContentExtracter _contentExtracter;
        private readonly ITableSchemaAnalyzer _schemaAnalyzer;

        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();
        private Subject<string> _updateOutputLog = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        public IObservable<string> OnOutputLogUpdated => _updateOutputLog;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);

        private void OnSourceProgressLevelUpdated(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void OnSourceProgressValueUpdated(float progressValue) => _updateProgressValue.OnNext(progressValue);

        public ExcelToMessagePackData(IDocumentContentExtracter contentExtracter, ITableSchemaAnalyzer schemaAnalyzer)
        {
            _contentExtracter = contentExtracter;
            _schemaAnalyzer = schemaAnalyzer;
            _validator = new MessagePackMappingValidator();

            SubscribeToIProgressNotifiers(_contentExtracter, _validator);
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

                var contents = await _contentExtracter.ExtractDocumentContentDataAsync(documentInfos);

                // Enum 이름→값 조회용 맵 구성
                var enumValueMap = BuildEnumValueMap(contents);

                var schemas = _schemaAnalyzer.AnalyzeFields(contents);

                if (schemas == null || !schemas.Any())
                {
                    UpdateProgressLevel("No data found to export.");
                    return false;
                }

                string baseOutputPath = Elder.DataForge.Properties.Settings.Default.OutputPath;
                if (string.IsNullOrEmpty(baseOutputPath))
                {
                    UpdateProgressLevel("Error: Output path is not configured in Settings.");
                    return false;
                }

                string outputPath = Path.Combine(baseOutputPath, "Data");
                if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

                var options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);

                for (int i = 0; i < schemas.Count; i++)
                {
                    var schema = schemas[i];
                    UpdateProgressLevel($"Syncing & Exporting Table: {schema.TableName} ({i + 1}/{schemas.Count})");

                    var serializedTable = new List<object[]>();

                    foreach (var row in schema.RawRows)
                    {
                        var rowArray = new object[schema.AnalyzedFields.Count];
                        foreach (var field in schema.AnalyzedFields)
                        {
                            object parsedValue = ParseValueBySchema(field, row, enumValueMap);
                            // 분석기가 정한 KeyIndex 순서대로 배열에 배치 (유니티 [Key(n)]와 일치)
                            rowArray[field.KeyIndex] = parsedValue;
                        }
                        serializedTable.Add(rowArray);
                    }

                    // 3. MessagePack 직렬화 실행
                    byte[] bin = MessagePackSerializer.Serialize(serializedTable, options);

                    if (!await _validator.ValidateAsync(bin, schema))
                    {
                        UpdateProgressLevel($"Export aborted due to validation failure: {schema.TableName}");
                        return false;
                    }

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

        private object ParseValueBySchema(AnalyzedField field, List<string> row, Dictionary<string, Dictionary<string, int>> enumValueMap)
        {
            if (field.IsList)
            {
                var list = new List<object>();
                string baseType = field.ManagedType.Replace("List<", "").Replace(">", "");
                foreach (var idx in field.ExcelIndices)
                {
                    if (row.Count > idx && !string.IsNullOrEmpty(row[idx]))
                        list.Add(ConvertToPrimitive(baseType, row[idx], enumValueMap));
                }
                return list;
            }

            int targetIdx = field.ExcelIndices[0];

            if (row.Count <= targetIdx || string.IsNullOrEmpty(row[targetIdx]))
            {
                return GetDefaultValueForPrimitive(field.ManagedType, enumValueMap);
            }

            return ConvertToPrimitive(field.ManagedType, row[targetIdx], enumValueMap);
        }

        private Dictionary<string, Dictionary<string, int>> BuildEnumValueMap(Dictionary<string, DocumentContentData> contents)
        {
            var map = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var doc in contents.Values)
            {
                if (doc is not ExcelContentData excelData) continue;
                foreach (var enumSchema in excelData.EnumSchemas)
                {
                    var entryMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entry in enumSchema.Entries)
                        entryMap[entry.Name] = entry.Value;
                    map[enumSchema.EnumName] = entryMap;
                }
            }
            return map;
        }

        private object ConvertToPrimitive(string type, string value, Dictionary<string, Dictionary<string, int>> enumValueMap)
        {
            return type.ToLower() switch
            {
                "int" or "int32" => int.Parse(value),
                "float" or "single" => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
                "long" or "int64" => long.Parse(value),
                "double" => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
                "bool" or "boolean" => bool.Parse(value),
                "string" => value,
                _ => ResolveEnumValue(type, value, enumValueMap)
            };
        }

        private object GetDefaultValueForPrimitive(string type, Dictionary<string, Dictionary<string, int>> enumValueMap)
        {
            return type.ToLower() switch
            {
                "int" or "int32" => 0,
                "float" or "single" => 0f,
                "long" or "int64" => 0L,
                "double" => 0d,
                "bool" or "boolean" => false,
                "string" => string.Empty,
                _ => enumValueMap.ContainsKey(type) ? 0 : 0
            };
        }

        private object ResolveEnumValue(string enumTypeName, string value, Dictionary<string, Dictionary<string, int>> enumValueMap)
        {
            // 숫자가 직접 입력된 경우
            if (int.TryParse(value, out int directInt))
                return directInt;

            // Enum 이름 조회
            if (enumValueMap.TryGetValue(enumTypeName, out var entryMap) &&
                entryMap.TryGetValue(value, out int enumInt))
                return enumInt;

            // 매핑 실패 시 0 반환
            return 0;
        }
    }
}