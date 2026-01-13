using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excels;
using System.Text;

namespace Elder.DataForge.Core.CodeGenerators.MemoryPack
{
    public enum GenerationMode
    {
        SharedDTO, // 공용 (string 사용)
        UnityDOD   // 유니티 최적화 (FixedString 사용)
    }

    // 파일 이름과 내용을 담는 레코드
    public record GeneratedSourceCode(string FileName, string Content);

    public class MemoryPackSourceGenerator : ISourceCodeGenerator
    {
        private const string Prefix = "MsgP";
        private const string DTOSuffix = "DTO";
        private const string DODSuffix = "DOD";
        // 여긴 나중에 에디터에서 네임스페이스를 저장할 수 있게 처리 
        // 
        private const string TargetDataNamespace = "Elder.Game.Resource.Data";
        private const string TargetConverterNamespace = "Elder.Game.Resource.Data.Convert";

        private string CreateStructName(string sheetName, string suffix)
        {
            return $"{Prefix}{sheetName}{suffix}"; 
        }

        public async Task<List<GeneratedSourceCode>> GenerateAsync(Dictionary<string, DocumentContentData> documentContents)
        {
            var generatedFiles = new List<GeneratedSourceCode>();

            foreach (var document in documentContents.Values)
            {
                // 엑셀 데이터가 아니면 스킵
                if (!(document is ExcelContentData excelData))
                    continue;

                // 시트 단위로 순회 (시트 1개 = 클래스 1개 = 파일 2개)
                foreach (var sheet in excelData.SheetDatas.Values)
                {
                    string dataName = CreateStructName(sheet.SheetName, DTOSuffix);
                    string modelContent = GenerateModelContent(dataName, sheet);
                    generatedFiles.Add(new GeneratedSourceCode($"{dataName}.cs", modelContent));
                }
            }

            return await Task.FromResult(generatedFiles);
        }

        private string GenerateModelContent(string dataName, in ExcelSheetData sheetData)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using MessagePack;"); // MessagePack 네임스페이스
            sb.AppendLine();

            sb.AppendLine($"namespace {TargetDataNamespace}");
            sb.AppendLine("{");
            sb.AppendLine("\t[MessagePackObject]");
            sb.AppendLine($"\tpublic readonly struct {dataName}");
            sb.AppendLine("\t{");

            // 1. 엑셀 컬럼 순서대로 정렬된 리스트 (Serialization용)
            var fieldGroups = sheetData.FieldDefinitions.GroupBy(x => x.VariableName)
                .Select(group => new {
                    Name = group.Key, 
                    Fields = group.ToList(),
                    // 이 그룹의 첫 번째 컬럼 번호를 '불변의 Key'로 사용 (하위 호환성)
                    StableKeyIndex = group.Min(f => f.FieldOrder) - 1,
                    // 그룹 내 모든 필드의 크기 합산 (예: int 3개면 4*3 = 12바이트)
                    TotalSize = group.Sum(f => GetTypeSize(f.VariableType))
                })
                // 2. 메모리 레이아웃 최적화: 총 크기가 큰 그룹부터 정렬
                .OrderByDescending(g => g.TotalSize)
                // 3. 크기가 같다면 StableKeyIndex 순으로 정렬 (결과 일관성)
                .ThenBy(g => g.StableKeyIndex)
                .ToList();

            // 4. 실제 코드 생성 로직
            foreach (var group in fieldGroups)
            {
                // 정렬과 관계없이 [Key]는 엑셀의 원래 위치를 유지하여 하위 호환성 확보
                sb.AppendLine($"\t\t[Key({group.StableKeyIndex})]");

                string csharpType = ConvertToCSharpType(group.Fields[0].VariableType, GenerationMode.SharedDTO);

                if (group.Fields.Count > 1)
                {
                    // List 타입인 경우 (DOD 모드라면 여기서 FixedList 등을 고려해야 함)
                    sb.AppendLine($"\t\tpublic readonly List<{csharpType}> {group.Name}; // Size: {group.TotalSize}");
                }
                else
                {
                    // 단일 필드인 경우
                    sb.AppendLine($"\t\tpublic readonly {csharpType} {group.Name}; // Size: {group.TotalSize}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("\t\t[SerializationConstructor]");
            sb.Append($"\t\tpublic {dataName}(");
            for (int i = 0; i < fieldGroups.Count; i++)
            {
                var group = fieldGroups[i];
                string csharpType = ConvertToCSharpType(group.Fields[0].VariableType, GenerationMode.SharedDTO);
                string typeStr = group.Fields.Count > 1 ? $"List<{csharpType}>" : csharpType;

                string paramName = char.ToLower(group.Name[0]) + group.Name.Substring(1);

                sb.Append($"{typeStr} {paramName}");
                if (i < fieldGroups.Count - 1) 
                    sb.Append(", "); // 마지막이 아니면 쉼표 추가
            }
            sb.AppendLine(")");
            sb.AppendLine("\t\t{");
            
            foreach (var group in fieldGroups)
            {
                string paramName = char.ToLower(group.Name[0]) + group.Name.Substring(1);
                sb.AppendLine($"\t\t\tthis.{group.Name} = {paramName};"); // 세미콜론 추가
            }
            
            sb.AppendLine("\t\t}");

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }
        private string GenerateParserContent(string dataName, in ExcelSheetData sheetData)
        {
            var sb = new StringBuilder();
            return sb.ToString();
        }
        private int GetTypeSize(string rawType)
        {
            string type = rawType.ToLower().Trim();
            return type switch
            {
                "double" or "long" or "int64" => 8,
                "int" or "int32" or "float" or "single" => 4,
                "short" => 2,
                "bool" or "byte" => 1,
                "string" or "str" => 64, // FixedString64Bytes 가정 시
                _ => 4 // Enum이나 기타 기본값
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
                    // MessagePack도 Unity에서 FixedString을 사용할 수 있지만, 
                    // 별도의 Formatter 등록이 필요할 수 있습니다.
                    return mode == GenerationMode.UnityDOD ? "FixedString64Bytes" : "string";
                default: return rawType;
            }
        }

        public void Dispose()
        {
            // 리소스 해제가 필요하다면 여기에 작성
        }
    }
}