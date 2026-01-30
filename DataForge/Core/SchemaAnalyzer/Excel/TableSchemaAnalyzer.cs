using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excel;
using Elder.DataForge.Core.CodeGenerators.MessagePack; // GenerationMode 참조용

namespace Elder.DataForge.Core.SchemaAnalyzer.Excel
{
    public class TableSchemaAnalyzer : ITableSchemaAnalyzer
    {
        public List<TableSchema> AnalyzeFields(Dictionary<string, DocumentContentData> documentContents)
        {
            var schemas = new List<TableSchema>();

            foreach (var document in documentContents.Values)
            {
                if (document is not ExcelContentData excelData) continue;

                foreach (var sheet in excelData.SheetDatas.Values)
                {
                    // 1. 시트의 필드 구조 분석
                    var analyzedFields = AnalyzeSheetFields(sheet);

                    // 2. 통합 설계도(TableSchema) 생성
                    var schema = new TableSchema
                    {
                        TableName = sheet.SheetName,
                        AnalyzedFields = analyzedFields,
                        RawRows = sheet.Rows // 실제 데이터 행들도 함께 저장 (바이너리 추출용)
                    };

                    schemas.Add(schema);
                }
            }

            return schemas;
        }

        private List<AnalyzedField> AnalyzeSheetFields(ExcelSheetData sheetData)
        {
            // 기존에 작성하신 분석 로직을 그대로 사용합니다.
            var groups = sheetData.FieldDefinitions.GroupBy(x => x.VariableName).ToList();
            var temp = groups.Select(g => {
                var first = g.First();
                string mBase = ConvertType(first.VariableType, GenerationMode.SharedDTO);
                string uBase = ConvertType(first.VariableType, GenerationMode.UnityDOD);
                bool isList = g.Count() > 1;
                string uType = isList ? GetFixedListType(uBase, g.Count()) : uBase;

                return new
                {
                    Name = g.Key,
                    MType = isList ? $"List<{mBase}>" : mBase,
                    UType = uType,
                    Size = isList ? GetFixedListSize(uType) : GetTypeSize(uBase),
                    IsList = isList,
                    Order = g.Min(f => f.FieldOrder),
                    Indices = g.Select(f => f.FieldOrder - 1).ToList()
                };
            }).OrderByDescending(x => x.Size).ThenBy(x => x.Order).ToList();

            return temp.Select((x, i) => new AnalyzedField(
                x.Name,
                char.ToLower(x.Name[0]) + x.Name.Substring(1),
                x.MType,
                x.UType,
                i, // KeyIndex
                x.Size,
                x.IsList,
                x.Indices
            )).ToList();
        }

        // --- 기존 헬퍼 메서드들 유지 ---
        private string ConvertType(string t, GenerationMode m) => t.ToLower() switch
        {
            "int" or "int32" => "int",
            "float" or "single" => "float",
            "string" or "str" => m == GenerationMode.UnityDOD ? "FixedString32Bytes" : "string",
            _ => t
        };

        private string GetFixedListType(string type, int count)
        {
            int size = (GetTypeSize(type) * count) + 2;
            return size <= 32 ? $"FixedList32Bytes<{type}>" : size <= 64 ? $"FixedList64Bytes<{type}>" : $"FixedList128Bytes<{type}>";
        }

        private int GetFixedListSize(string t) => t.Contains("32") ? 32 : t.Contains("64") ? 64 : 128;
        private int GetTypeSize(string t) => t.Contains("64") || t == "double" ? 8 : t.Contains("32") || t == "float" ? 4 : 1;
    }
}