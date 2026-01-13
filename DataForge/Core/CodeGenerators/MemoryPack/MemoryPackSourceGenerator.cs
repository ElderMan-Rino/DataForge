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
        private const string ClassPrefix = "MP";
        private const string ClassSuffix = "Data";
        private const string TargetDataNamespace = "Elder.Game.Resource.Data";
        private const string TargetConverterNamespace = "Elder.Game.Resource.Data.Convert";

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
                    string className = $"{ClassPrefix}{sheet.SheetName}{ClassSuffix}";

                    //// 1. [공용] 데이터 모델 파일 생성 (.cs)
                    //// - MemoryPackable 속성 포함
                    //// - 순수 데이터 프로퍼티만 포함
                    //string modelContent = GenerateModelContent(className, sheet);
                    //generatedFiles.Add(new GeneratedSourceCode($"{className}.cs", modelContent));
                    
                    //// 2. [툴 전용] 파서 로직 파일 생성 (.Parser.cs)
                    //// - Parse 메서드 포함
                    //// - 툴 프로젝트에만 포함시키고, 유니티에는 넣지 않음
                    //string parserContent = GenerateParserContent(className, sheet);
                    //generatedFiles.Add(new GeneratedSourceCode($"{className}.Parser.cs", parserContent));
                }
            }

            return await Task.FromResult(generatedFiles);
        }

        private string GenerateModelContent(string className, ExcelSheetData sheetData)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using MemoryPack;"); // MemoryPack 필수
            sb.AppendLine();
            sb.AppendLine($"namespace {TargetDataNamespace}");
            sb.AppendLine("{");
            sb.AppendLine("\t[MemoryPackable]"); // 데이터 모델에만 붙입니다.
            sb.AppendLine($"\tpublic partial class {className}");
            sb.AppendLine("\t{");

            // 필드 정의
            var groupedFields = sheetData.FieldDefinitions.GroupBy(x => x.VariableName);

            foreach (var group in groupedFields)
            {
                var fieldDef = group.First();
                string fieldName = fieldDef.VariableName;
                string csharpType = ConvertToCSharpType(fieldDef.VariableType, GenerationMode.SharedDTO);

                if (group.Count() > 1)
                {
                    // [List]
                    sb.AppendLine($"\t\tpublic List<{csharpType}> {fieldName} {{ get; set; }}");
                }
                else
                {
                    // [Single]
                    sb.AppendLine($"\t\tpublic {csharpType} {fieldName} {{ get; set; }}");
                }
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            return sb.ToString();
        }

        // ==================================================================================
        // 2. 파서 로직 생성 로직 (Parser Generator)
        // ==================================================================================
        private string GenerateParserContent(string className, ExcelSheetData sheetData)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine();
            sb.AppendLine($"namespace {TargetDataNamespace}");
            sb.AppendLine("{");
            // [MemoryPackable] 제거 (이미 Model 파일에 있음)
            // partial class로 선언하여 Model 파일과 합쳐짐
            sb.AppendLine($"\tpublic partial class {className}");
            sb.AppendLine("\t{");

            // Parse 메서드 생성
            sb.AppendLine($"\t\tpublic static {className} Parse(List<string> rowData)");
            sb.AppendLine("\t\t{");
            sb.AppendLine($"\t\t\tvar instance = new {className}();");

            var groupedFields = sheetData.FieldDefinitions.GroupBy(x => x.VariableName);

            foreach (var group in groupedFields)
            {
                var fieldDef = group.First();
                string fieldName = fieldDef.VariableName;
                string csharpType = ConvertToCSharpType(fieldDef.VariableType, GenerationMode.SharedDTO);

                if (group.Count() > 1)
                {
                    // [List 파싱]
                    sb.AppendLine($"\t\t\tinstance.{fieldName} = new List<{csharpType}>();");
                    foreach (var colDef in group)
                    {
                        // 엑셀은 1부터 시작하므로 -1 하여 0-based index로 맞춤
                        int colIndex = colDef.FieldOrder - 1;
                        string valueCode = $"rowData[{colIndex}]";
                        string parseLogic = GenerateParseSyntax(csharpType, valueCode);

                        // 인덱스 범위 및 빈 값 체크
                        sb.AppendLine($"\t\t\tif (rowData.Count > {colIndex} && !string.IsNullOrEmpty({valueCode}))");
                        sb.AppendLine($"\t\t\t{{");
                        sb.AppendLine($"\t\t\t\tinstance.{fieldName}.Add({parseLogic});");
                        sb.AppendLine($"\t\t\t}}");
                    }
                }
                else
                {
                    // [Single 파싱]
                    int colIndex = fieldDef.FieldOrder - 1;
                    string valueCode = $"rowData[{colIndex}]";
                    string parseLogic = GenerateParseSyntax(csharpType, valueCode);

                    sb.AppendLine($"\t\t\tif (rowData.Count > {colIndex} && !string.IsNullOrEmpty({valueCode}))");
                    sb.AppendLine($"\t\t\t{{");
                    sb.AppendLine($"\t\t\t\tinstance.{fieldName} = {parseLogic};");
                    sb.AppendLine($"\t\t\t}}");
                }
            }

            sb.AppendLine("\t\t\treturn instance;");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine("}");

            return sb.ToString();
        }

        // ==================================================================================
        // 3. 헬퍼 메서드 (타입 변환 및 구문 생성)
        // ==================================================================================

        // 엑셀 타입(string) -> C# 타입(string) 변환
        private string ConvertToCSharpType(string rawType, GenerationMode mode)
        {
            string type = rawType.ToLower().Trim();

            // 1. 공통 타입 처리 (int, float, bool 등)
            switch (type)
            {
                case "int":
                case "int32": return "int";
                case "long":
                case "int64": return "long";
                case "float":
                case "single": return "float";
                case "double": return "double";
                case "bool":
                case "boolean": return "bool";

                // 2. 환경에 따라 달라지는 타입 처리 (핵심!)
                case "string":
                case "str":
                    return mode == GenerationMode.UnityDOD ? "FixedString64Bytes" : "string";

                default:
                    return rawType; // Enum 등은 그대로 반환
            }
        }

        // 타입별 파싱 코드(int.Parse 등) 생성
        private string GenerateParseSyntax(string type, string valueVariable)
        {
            switch (type)
            {
                case "int":
                    return $"int.Parse({valueVariable})";
                case "long":
                    return $"long.Parse({valueVariable})";
                case "float":
                    return $"float.Parse({valueVariable})";
                case "double":
                    return $"double.Parse({valueVariable})";
                case "bool":
                    return $"bool.Parse({valueVariable})";
                case "string":
                    return valueVariable; // 문자열은 변환 불필요
                default:
                    // Enum이나 기타 타입 대응이 필요하면 여기에 추가
                    // 예: return $"Enum.Parse<{type}>({valueVariable})";
                    return valueVariable;
            }
        }

        public void Dispose()
        {
            // 리소스 해제가 필요하다면 여기에 작성
        }
    }
}