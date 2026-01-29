using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using System.Reactive;
using System.Text;

namespace Elder.DataForge.Core.CodeGenerators.MessagePack
{
    public enum GenerationMode { SharedDTO, UnityDOD }

    public record GeneratedSourceCode(string FileName, string Content);

    public class MessagePackSourceGenerator : ISourceCodeGenerator
    {
        private const string Prefix = "MsgP";
        private const string DTOSuffix = "DTO";
        private const string DODSuffix = "DOD";
        private const string TargetDataNamespace = "Elder.Game.Resource.Data";
        private const string TargetParserNamespace = "Elder.Game.Resource.Data.Convert";
        private const string EL = "\r\n"; // Windows 표준 줄 바꿈 강제

        private void WriteLine(StringBuilder sb, string text = "") => sb.Append(text).Append(EL);

        public async Task<List<GeneratedSourceCode>> GenerateAsync(List<TableSchema> schemas)
        {
            var generatedFiles = new List<GeneratedSourceCode>();
            foreach (var schema in schemas)
            {
                var dtoName = $"{Prefix}{schema.TableName}{DTOSuffix}";
                var dodName = $"{Prefix}{schema.TableName}{DODSuffix}";

                generatedFiles.Add(new GeneratedSourceCode($"{dtoName}.cs", GenerateModelContent(dtoName, schema.AnalyzedFields)));
                generatedFiles.Add(new GeneratedSourceCode($"{dtoName}.Parser.cs", GenerateParserContent(dtoName, schema.AnalyzedFields)));
                generatedFiles.Add(new GeneratedSourceCode($"{dodName}.cs", GenerateRuntimeContent(dodName, schema.AnalyzedFields)));
            }
            return await Task.FromResult(generatedFiles);
        }

        private string GenerateModelContent(string name, List<AnalyzedField> fields)
        {
            var sb = new StringBuilder();
            WriteLine(sb, "using System;\nusing System.Collections.Generic;\nusing MessagePack;\n");
            WriteLine(sb, $"namespace {TargetDataNamespace}\n{{");
            WriteLine(sb, "\t[MessagePackObject]");
            WriteLine(sb, $"\tpublic readonly struct {name}\n\t{{");
            foreach (var f in fields) 
                WriteLine(sb, $"\t\t[Key({f.KeyIndex})] public readonly {f.ManagedType} {f.Name};");
            WriteLine(sb, "\n\t\t[SerializationConstructor]");
            sb.Append($"\t\tpublic {name}(").Append(string.Join(", ", fields.Select(f => $"{f.ManagedType} {f.PropertyName}"))).AppendLine(")");
            WriteLine(sb, "\t\t{");
            foreach (var f in fields) 
                WriteLine(sb, $"\t\t\tthis.{f.Name} = {f.PropertyName};");
            WriteLine(sb, "\t\t}\n\t}\n}");
            return sb.ToString();
        }

        private string GenerateRuntimeContent(string dodName, List<AnalyzedField> fields)
        {
            var sb = new StringBuilder();
            WriteLine(sb, "using System;\nusing Unity.Collections;\nusing Unity.Entities;\n");
            WriteLine(sb, $"namespace {TargetDataNamespace}\n{{");

            // 1. DOD 행 구조체
            WriteLine(sb, "\t[Serializable]");
            WriteLine(sb, $"\tpublic readonly struct {dodName}\n\t{{");
            foreach (var f in fields)
            {
                string type = f.ManagedType == "string" ? "BlobString" : f.UnmanagedType;
                WriteLine(sb, $"\t\tpublic readonly {type} {f.Name};");
            }
            // (생성자 생략...)
            WriteLine(sb, "\t}\n");

            // 2. BlobTable 루트 구조체 (여기 위치하는 것이 적절합니다)
            var tableName = dodName.Replace(DODSuffix, "BlobTable");
            WriteLine(sb, "\t/// <summary> Blob Asset 루트 구조체 </summary>");
            WriteLine(sb, $"\tpublic readonly struct {tableName}\n\t{{");
            WriteLine(sb, $"\t\tpublic readonly BlobArray<{dodName}> Rows;"); // DOD 타입을 참조
            WriteLine(sb, $"\n\t\tpublic {tableName}(BlobArray<{dodName}> rows) => this.Rows = rows;");
            WriteLine(sb, "\t}\n}");
            return sb.ToString();
        }

        private string GenerateParserContent(string dtoName, List<AnalyzedField> fields)
        {
            var sb = new StringBuilder();
            WriteLine(sb, $"using {TargetDataNamespace};\nusing System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing MessagePack;\n");
            WriteLine(sb, $"namespace {TargetParserNamespace}\n{{");
            WriteLine(sb, $"\tpublic static class {dtoName}Parser\n\t{{");
            WriteLine(sb, $"\t\tpublic static {dtoName} ParseRow(List<string> rowData)\n\t\t{{");
            foreach (var f in fields)
            {
                string bType = f.ManagedType.Replace("List<", "").Replace(">", "");
                if (f.IsList)
                {
                    WriteLine(sb, $"\t\t\tvar {f.PropertyName} = new {f.ManagedType}();");
                    foreach (var idx in f.ExcelIndices) WriteLine(sb, $"\t\t\tif (rowData.Count > {idx} && !string.IsNullOrEmpty(rowData[{idx}])) {f.PropertyName}.Add({GetParseSyntax(bType, $"rowData[{idx}]")});");
                }
                else
                {
                    WriteLine(sb, $"\t\t\tvar {f.PropertyName} = (rowData.Count > {f.ExcelIndices[0]} && !string.IsNullOrEmpty(rowData[{f.ExcelIndices[0]}])) ? {GetParseSyntax(bType, $"rowData[{f.ExcelIndices[0]}]")} : default;");
                }
            }
            sb.Append($"\n\t\t\treturn new {dtoName}(").Append(string.Join(", ", fields.Select(f => f.PropertyName))).AppendLine(");");
            WriteLine(sb, "\t\t}\n\t}\n}");
            return sb.ToString();
        }

        private string GetParseSyntax(string type, string expr) => type switch
        {
            "int" => $"int.Parse({expr})",
            "long" => $"long.Parse({expr})",
            "float" => $"float.Parse({expr}, System.Globalization.CultureInfo.InvariantCulture)",
            "bool" => $"bool.Parse({expr})",
            "string" => expr,
            _ => $"(({type})Enum.Parse(typeof({type}), {expr}))"
        };
    }
}