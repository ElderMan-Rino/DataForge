using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excel;
using Elder.DataForge.Core.CodeGenerator.MessagePack;
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
                    var filteredRows = new List<List<string>>();
                    foreach (var row in sheet.Rows)
                    {
                        if (row == null || row.Count == 0) continue;

                        bool hasSummaryTag = row.Any(cell =>
                            cell != null &&
                            cell.Trim().Equals("</summary>", StringComparison.OrdinalIgnoreCase)
                        );

                        if (hasSummaryTag) continue;

                        filteredRows.Add(row);
                    }

                    var schema = new TableSchema
                    {
                        TableName = sheet.SheetName,
                        AnalyzedFields = analyzedFields,
                        RawRows = filteredRows // 필터링된 행들만 할당
                    };

                    schemas.Add(schema);
                }
            }

            return schemas;
        }

        private List<AnalyzedField> AnalyzeSheetFields(ExcelSheetData sheetData)
        {
            var groups = sheetData.FieldDefinitions.GroupBy(x => x.VariableName).ToList();
            var fieldList = groups.Select((g, index) =>
            {
                var first = g.First();

                string mBase = ConvertType(first.VariableType, GenerationMode.SharedDTO);
                string uBase = ConvertType(first.VariableType, GenerationMode.UnityDOD);

                bool isList = g.Count() > 1;

                string uType = isList ? $"Unity.Entities.BlobArray<{uBase}>" : uBase;

                return new
                {
                    Name = g.Key,
                    MType = isList ? $"List<{mBase}>" : mBase,
                    UType = uType,
                    Size = isList ? 8 : GetTypeSize(uBase),
                    IsList = isList,
                    Order = g.Min(f => f.FieldOrder),
                    Indices = g.Select(f => f.FieldOrder - 1).ToList(),
                    OriginalIndex = index // ✨ 엑셀 시트 정의 원래 순서
                };
            }).ToList();

            var optimizedLayout = fieldList
                .OrderByDescending(x => x.Size)
                .ThenBy(x => x.Order)
                .ToList();

            return optimizedLayout.Select((x, sortedIndex) => new AnalyzedField(
                x.Name,
                char.ToLower(x.Name[0]) + x.Name.Substring(1),
                x.MType,
                x.UType,
                sortedIndex, // KeyIndex = 정렬 후 위치 (MessagePack [Key(n)]과 일치)
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
            "string" or "str" => m == GenerationMode.UnityDOD ? "BlobString" : "string",
            _ => t
        };

        private int GetTypeSize(string t) => t switch
        {
            "double" or "long" => 8,
            "int" or "float" => 4,
            "bool" => 1,
            "BlobString" => 8,
            _ => 4
        };
    }
}