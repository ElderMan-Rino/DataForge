using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excels;
using System.Text;

namespace Elder.DataForge.Core.CodeGenerators.MessagePack
{
    public enum GenerationMode
    {
        SharedDTO, // 공용 (Managed: string, List 사용)
        UnityDOD   // 유니티 최적화 (Unmanaged: FixedString, FixedList 사용)
    }

    public record GeneratedSourceCode(string FileName, string Content);

    public class MessagePackSourceGenerator : ISourceCodeGenerator
    {
        private const string Prefix = "MsgP";
        private const string DTOSuffix = "DTO";
        private const string DODSuffix = "DOD";

        // 환경 설정에 따라 변경 가능하도록 처리될 네임스페이스
        private const string TargetDataNamespace = "Elder.Game.Resource.Data";
        private const string TargetParserNamespace = "Elder.Game.Resource.Data.Convert";

        private string CreateStructName(string sheetName, string suffix)
        {
            return $"{Prefix}{sheetName}{suffix}";
        }

        public async Task<List<GeneratedSourceCode>> GenerateAsync(Dictionary<string, DocumentContentData> documentContents)
        {
            var generatedFiles = new List<GeneratedSourceCode>();

            foreach (var document in documentContents.Values)
            {
                if (!(document is ExcelContentData excelData))
                    continue;

                foreach (var sheet in excelData.SheetDatas.Values)
                {
                    var dtoName = CreateStructName(sheet.SheetName, DTOSuffix);
                    var dodName = CreateStructName(sheet.SheetName, DODSuffix);

                    // 1. DTO 생성 (MessagePack Serialization용)
                    generatedFiles.Add(new GeneratedSourceCode($"{dtoName}.cs", GenerateModelContent(dtoName, sheet)));

                    // 2. Parser 생성 (Excel String -> DTO 변환 및 직렬화용)
                    generatedFiles.Add(new GeneratedSourceCode($"{dtoName}Parser.cs", GenerateParserContent(dtoName, sheet)));

                    // 3. DOD 생성 (Unity DOTS/Jobs 최적화용 고성능 구조체)
                    generatedFiles.Add(new GeneratedSourceCode($"{dodName}.cs", GenerateRuntimeContent(dodName, sheet)));
                }
            }

            return await Task.FromResult(generatedFiles);
        }

        /// <summary>
        /// Unity DOTS용 DOD 구조체 생성 (Unmanaged/Blittable)
        /// </summary>
        private string GenerateRuntimeContent(string dataName, in ExcelSheetData sheetData)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using Unity.Collections;"); // DOD에서는 List가 없으므로 Generic은 생략 가능
            sb.AppendLine();

            sb.AppendLine($"namespace {TargetDataNamespace}");
            sb.AppendLine("{");
            sb.AppendLine("\t[Serializable]");
            // 직접 역직렬화를 위해 MessagePackObject 어노테이션이 필요할 수 있습니다.
            sb.AppendLine("\t[MessagePack.MessagePackObject]");
            sb.AppendLine($"\tpublic readonly struct {dataName}");
            sb.AppendLine("\t{");

            // 필드 분석 (Size 기준 내림차순 정렬)
            var fieldGroups = sheetData.FieldDefinitions.GroupBy(x => x.VariableName)
                .Select(group => {
                    string baseType = ConvertToCSharpType(group.First().VariableType, GenerationMode.UnityDOD);
                    bool isList = group.Count() > 1;
                    string finalType = isList ? GetSuitableFixedListType(baseType, group.Count()) : baseType;
                    int totalSize = isList ? GetFixedListFullSize(finalType) : GetTypeSize(baseType);
                    int stableKey = group.Min(f => f.FieldOrder) - 1;

                    return new { Name = group.Key, FinalType = finalType, TotalSize = totalSize, Key = stableKey };
                })
                .OrderByDescending(g => g.TotalSize).ToList();

            // 1. 필드 정의 및 MessagePack Key 부여
            foreach (var group in fieldGroups)
            {
                sb.AppendLine($"\t\t[MessagePack.Key({group.Key})]");
                sb.AppendLine($"\t\tpublic readonly {group.FinalType} {group.Name}; // Size: {group.TotalSize}");
            }

            sb.AppendLine();

            // 2. 일반 생성자 (직접 역직렬화 시 MessagePack이 사용하거나 초기화 시 사용)
            sb.AppendLine("\t\t[MessagePack.SerializationConstructor]");
            sb.Append($"\t\tpublic {dataName}(");
            for (int i = 0; i < fieldGroups.Count; i++)
            {
                sb.Append($"{fieldGroups[i].FinalType} {char.ToLower(fieldGroups[i].Name[0]) + fieldGroups[i].Name.Substring(1)}");
                if (i < fieldGroups.Count - 1) sb.Append(", ");
            }
            sb.AppendLine(")");
            sb.AppendLine("\t\t{");
            foreach (var group in fieldGroups)
                sb.AppendLine($"\t\t\tthis.{group.Name} = {char.ToLower(group.Name[0]) + group.Name.Substring(1)};");
            sb.AppendLine("\t\t}");

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateModelContent(string dataName, in ExcelSheetData sheetData)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using MessagePack;");
            sb.AppendLine();

            sb.AppendLine($"namespace {TargetDataNamespace}");
            sb.AppendLine("{");
            sb.AppendLine("\t[MessagePackObject]");
            sb.AppendLine($"\tpublic readonly struct {dataName}");
            sb.AppendLine("\t{");

            var fieldGroups = sheetData.FieldDefinitions.GroupBy(x => x.VariableName)
                .Select(group => new {
                    Name = group.Key,
                    Fields = group.ToList(),
                    StableKeyIndex = group.Min(f => f.FieldOrder) - 1,
                    TotalSize = group.Sum(f => GetTypeSize(f.VariableType))
                })
                .OrderByDescending(g => g.TotalSize)
                .ThenBy(g => g.StableKeyIndex)
                .ToList();

            foreach (var group in fieldGroups)
            {
                sb.AppendLine($"\t\t[Key({group.StableKeyIndex})]");
                string csharpType = ConvertToCSharpType(group.Fields[0].VariableType, GenerationMode.SharedDTO);
                if (group.Fields.Count > 1)
                    sb.AppendLine($"\t\tpublic readonly List<{csharpType}> {group.Name};");
                else
                    sb.AppendLine($"\t\tpublic readonly {csharpType} {group.Name};");
            }

            sb.AppendLine();
            sb.AppendLine("\t\t[SerializationConstructor]");
            sb.Append($"\t\tpublic {dataName}(");
            for (int i = 0; i < fieldGroups.Count; i++)
            {
                string csharpType = ConvertToCSharpType(fieldGroups[i].Fields[0].VariableType, GenerationMode.SharedDTO);
                string typeStr = fieldGroups[i].Fields.Count > 1 ? $"List<{csharpType}>" : csharpType;
                sb.Append($"{typeStr} {char.ToLower(fieldGroups[i].Name[0]) + fieldGroups[i].Name.Substring(1)}");
                if (i < fieldGroups.Count - 1) sb.Append(", ");
            }
            sb.AppendLine(")");
            sb.AppendLine("\t\t{");
            foreach (var group in fieldGroups)
                sb.AppendLine($"\t\t\tthis.{group.Name} = {char.ToLower(group.Name[0]) + group.Name.Substring(1)};");
            sb.AppendLine("\t\t}");

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateParserContent(string dtoName, ExcelSheetData sheetData)
        {
            string parserName = $"{dtoName}Parser";
            var sb = new StringBuilder();
            sb.AppendLine($"using {TargetDataNamespace};");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using MessagePack;");
            sb.AppendLine();

            sb.AppendLine($"namespace {TargetParserNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic static class {parserName}");
            sb.AppendLine("\t{");

            sb.AppendLine($"\t\tpublic static {dtoName} ParseRow(List<string> rowData)");
            sb.AppendLine("\t\t{");

            var groupedFields = sheetData.FieldDefinitions.GroupBy(x => x.VariableName).ToList();
            foreach (var group in groupedFields)
            {
                string csharpType = ConvertToCSharpType(group.First().VariableType, GenerationMode.SharedDTO);
                string varName = char.ToLower(group.Key[0]) + group.Key.Substring(1);

                if (group.Count() > 1)
                {
                    sb.AppendLine($"\t\t\tvar {varName} = new List<{csharpType}>();");
                    foreach (var field in group)
                    {
                        int idx = field.FieldOrder - 1;
                        sb.AppendLine($"\t\t\tif (rowData.Count > {idx} && !string.IsNullOrEmpty(rowData[{idx}])) {varName}.Add({GenerateParseSyntax(csharpType, $"rowData[{idx}]")});");
                    }
                }
                else
                {
                    int idx = group.First().FieldOrder - 1;
                    string parseLogic = GenerateParseSyntax(csharpType, $"rowData[{idx}]");
                    sb.AppendLine($"\t\t\tvar {varName} = (rowData.Count > {idx} && !string.IsNullOrEmpty(rowData[{idx}])) ? {parseLogic} : default;");
                }
            }

            sb.Append($"\t\t\treturn new {dtoName}(");
            for (int i = 0; i < groupedFields.Count; i++)
            {
                sb.Append(char.ToLower(groupedFields[i].Key[0]) + groupedFields[i].Key.Substring(1));
                if (i < groupedFields.Count - 1) sb.Append(", ");
            }
            sb.AppendLine(");");
            sb.AppendLine("\t\t}");

            sb.AppendLine();
            sb.AppendLine($"\t\tpublic static byte[] SerializeSheet(List<List<string>> allRows)");
            sb.AppendLine("\t\t{");
            sb.AppendLine($"\t\t\tvar dtos = allRows.Select(row => ParseRow(row)).ToList();");
            sb.AppendLine("\t\t\treturn MessagePackSerializer.Serialize(dtos);");
            sb.AppendLine("\t\t}");

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateParseSyntax(string csharpType, string inputExpression)
        {
            return csharpType switch
            {
                "int" => $"int.Parse({inputExpression})",
                "long" => $"long.Parse({inputExpression})",
                "float" => $"float.Parse({inputExpression}, System.Globalization.CultureInfo.InvariantCulture)",
                "double" => $"double.Parse({inputExpression}, System.Globalization.CultureInfo.InvariantCulture)",
                "bool" => $"bool.Parse({inputExpression})",
                "string" => inputExpression,
                _ => $"(({csharpType})Enum.Parse(typeof({csharpType}), {inputExpression}))"
            };
        }

        private string GetSuitableFixedListType(string csharpType, int count)
        {
            int elementSize = GetTypeSize(csharpType);
            int totalRequiredBytes = (elementSize * count) + 2; // +2 for Length header

            if (totalRequiredBytes <= 32) 
                return $"FixedList32Bytes<{csharpType}>";
            if (totalRequiredBytes <= 64)
                return $"FixedList64Bytes<{csharpType}>";
            if (totalRequiredBytes <= 128) 
                return $"FixedList128Bytes<{csharpType}>";
            return $"FixedList128Bytes<{csharpType}>";
        }

        private int GetFixedListFullSize(string fixedListTypeName)
        {
            if (fixedListTypeName.Contains("32"))
                return 32;
            if (fixedListTypeName.Contains("64"))
                return 64;
            return 128;
        }

        private int GetTypeSize(string rawType)
        {
            string type = rawType.ToLower().Trim();
            if (type.Contains("fixedstring64")) return 64;
            if (type.Contains("fixedstring32")) return 32;

            return type switch
            {
                "double" or "long" or "int64" => 8,
                "int" or "int32" or "float" or "single" => 4,
                "short" => 2,
                "bool" or "byte" => 1,
                "string" or "str" => 32, // DOD 모드 기본값
                _ => 4
            };
        }

        private string ConvertToCSharpType(string rawType, GenerationMode mode)
        {
            string type = rawType.ToLower().Trim();
            switch (type)
            {
                case "int": case "int32": return "int";
                case "long": case "int64": return "long";
                case "float": case "single": return "float";
                case "double": return "double";
                case "bool": case "boolean": return "bool";
                case "string":
                case "str":
                    return mode == GenerationMode.UnityDOD ? "FixedString32Bytes" : "string";
                default: return rawType;
            }
        }

        public void Dispose() { }
    }
}