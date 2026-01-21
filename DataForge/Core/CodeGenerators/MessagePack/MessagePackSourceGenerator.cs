using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excels;
using System.Text;
using System.Linq;

namespace Elder.DataForge.Core.CodeGenerators.MessagePack
{
    public enum GenerationMode { SharedDTO, UnityDOD }

    public record GeneratedSourceCode(string FileName, string Content);

    internal record AnalyzedField(
        string Name, string PropertyName, string ManagedType, string UnmanagedType,
        int KeyIndex, int TotalSize, bool IsList, List<int> ExcelIndices);

    public class MessagePackSourceGenerator : ISourceCodeGenerator
    {
        private const string Prefix = "MsgP";
        private const string DTOSuffix = "DTO";
        private const string DODSuffix = "DOD";
        private const string TargetDataNamespace = "Elder.Game.Resource.Data";
        private const string TargetParserNamespace = "Elder.Game.Resource.Data.Convert";
        private const string EL = "\r\n"; // Windows 표준 줄 바꿈 강제

        private void WriteLine(StringBuilder sb, string text = "") => sb.Append(text).Append(EL);

        public async Task<List<GeneratedSourceCode>> GenerateAsync(Dictionary<string, DocumentContentData> documentContents)
        {
            var generatedFiles = new List<GeneratedSourceCode>();
            foreach (var document in documentContents.Values)
            {
                if (!(document is ExcelContentData excelData)) continue;
                foreach (var sheet in excelData.SheetDatas.Values)
                {
                    var fields = AnalyzeFields(sheet);
                    var dtoName = $"{Prefix}{sheet.SheetName}{DTOSuffix}";
                    var dodName = $"{Prefix}{sheet.SheetName}{DODSuffix}";

                    generatedFiles.Add(new GeneratedSourceCode($"{dtoName}.cs", GenerateModelContent(dtoName, fields)));
                    generatedFiles.Add(new GeneratedSourceCode($"{dtoName}.Parser.cs", GenerateParserContent(dtoName, fields)));
                    generatedFiles.Add(new GeneratedSourceCode($"{dodName}.cs", GenerateRuntimeContent(dodName, fields)));
                }
            }
            return await Task.FromResult(generatedFiles);
        }

        private List<AnalyzedField> AnalyzeFields(ExcelSheetData sheetData)
        {
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
                x.Name, char.ToLower(x.Name[0]) + x.Name.Substring(1), x.MType, x.UType, i, x.Size, x.IsList, x.Indices
            )).ToList();
        }

        private string GenerateModelContent(string name, List<AnalyzedField> fields)
        {
            var sb = new StringBuilder();
            WriteLine(sb, "using System;\nusing System.Collections.Generic;\nusing MessagePack;\n");
            WriteLine(sb, $"namespace {TargetDataNamespace}\n{{");
            WriteLine(sb, "\t[MessagePackObject]");
            WriteLine(sb, $"\tpublic readonly struct {name}\n\t{{");
            foreach (var f in fields) WriteLine(sb, $"\t\t[Key({f.KeyIndex})] public readonly {f.ManagedType} {f.Name};");
            WriteLine(sb, "\n\t\t[SerializationConstructor]");
            sb.Append($"\t\tpublic {name}(").Append(string.Join(", ", fields.Select(f => $"{f.ManagedType} {f.PropertyName}"))).AppendLine(")");
            WriteLine(sb, "\t\t{");
            foreach (var f in fields) WriteLine(sb, $"\t\t\tthis.{f.Name} = {f.PropertyName};");
            WriteLine(sb, "\t\t}\n\t}\n}");
            return sb.ToString();
        }

        private string GenerateRuntimeContent(string name, List<AnalyzedField> fields)
        {
            var sb = new StringBuilder();
            WriteLine(sb, "using System;\nusing Unity.Collections;\n");
            WriteLine(sb, $"namespace {TargetDataNamespace}\n{{");
            WriteLine(sb, "\t[Serializable]\n\t[MessagePack.MessagePackObject]");
            WriteLine(sb, $"\tpublic readonly struct {name}\n\t{{");
            foreach (var f in fields) WriteLine(sb, $"\t\t[MessagePack.Key({f.KeyIndex})] public readonly {f.UnmanagedType} {f.Name}; // Size: {f.TotalSize}");
            WriteLine(sb, "\n\t\t[MessagePack.SerializationConstructor]");
            sb.Append($"\t\tpublic {name}(").Append(string.Join(", ", fields.Select(f => $"{f.UnmanagedType} {f.PropertyName}"))).AppendLine(")");
            WriteLine(sb, "\t\t{");
            foreach (var f in fields) WriteLine(sb, $"\t\t\tthis.{f.Name} = {f.PropertyName};");
            WriteLine(sb, "\t\t}\n\t}\n}");
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

        private string GetFixedListType(string type, int count)
        {
            int size = (GetTypeSize(type) * count) + 2;
            return size <= 32 ? $"FixedList32Bytes<{type}>" : size <= 64 ? $"FixedList64Bytes<{type}>" : $"FixedList128Bytes<{type}>";
        }
        private int GetFixedListSize(string t) => t.Contains("32") ? 32 : t.Contains("64") ? 64 : 128;
        private int GetTypeSize(string t) => t.Contains("64") || t == "double" ? 8 : t.Contains("32") || t == "float" ? 4 : 1;
        private string ConvertType(string t, GenerationMode m) => t.ToLower() switch
        {
            "int" or "int32" => "int",
            "float" or "single" => "float",
            "string" or "str" => m == GenerationMode.UnityDOD ? "FixedString32Bytes" : "string",
            _ => t
        };
        public void Dispose() { }
    }
}