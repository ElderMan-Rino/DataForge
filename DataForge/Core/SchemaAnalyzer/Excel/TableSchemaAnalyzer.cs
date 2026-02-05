using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excel;
using Elder.DataForge.Core.CodeGenerators.MessagePack;
using System.Collections.Generic;
using System.Linq;

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
                    var analyzedFields = AnalyzeSheetFields(sheet);

                    var schema = new TableSchema
                    {
                        TableName = sheet.SheetName,
                        AnalyzedFields = analyzedFields,
                        RawRows = sheet.Rows
                    };

                    schemas.Add(schema);
                }
            }

            return schemas;
        }

        private List<AnalyzedField> AnalyzeSheetFields(ExcelSheetData sheetData)
        {
            var groups = sheetData.FieldDefinitions.GroupBy(x => x.VariableName).ToList();

            var temp = groups.Select(g => {
                var first = g.First();

                // 1. 기본 타입 결정
                string mBase = ConvertType(first.VariableType, GenerationMode.SharedDTO);
                string uBase = ConvertType(first.VariableType, GenerationMode.UnityDOD);

                bool isList = g.Count() > 1;

                // 2. UnityDOD용 타입 결정 (FixedList -> BlobArray)
                string uType = isList ? $"Unity.Entities.BlobArray<{uBase}>" : uBase;

                return new
                {
                    Name = g.Key,
                    MType = isList ? $"List<{mBase}>" : mBase,
                    UType = uType,
                    // 3. 사이즈 계산 (Blob은 참조값이므로 포인터 사이즈인 8바이트로 계산)
                    Size = isList ? 8 : GetTypeSize(uBase),
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

        private string ConvertType(string t, GenerationMode m) => t.ToLower() switch
        {
            "int" or "int32" => "int",
            "float" or "single" => "float",
            "double" => "double",
            "long" or "int64" => "long",
            "bool" or "boolean" => "bool",
            // string은 UnityDOD일 때 우리가 만든 BlobString 사용
            "string" or "str" => m == GenerationMode.UnityDOD ? "BlobString" : "string",
            _ => t
        };

        // Blob 기반에서는 리스트의 데이터 개수와 상관없이 구조체 내부 크기는 참조 정보(OffsetPtr) 사이즈임
        private int GetTypeSize(string t) => t switch
        {
            "double" or "long" => 8,
            "int" or "float" => 4,
            "bool" => 1,
            "BlobString" => 8, // BlobString 내부에도 BlobPtr이 들어감
            _ => 4 // 기본 포인터나 Enum 등
        };
    }
}