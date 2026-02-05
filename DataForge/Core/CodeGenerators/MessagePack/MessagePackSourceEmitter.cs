using Elder.DataForge.Core.Common.Const.MemoryPack;
using Elder.DataForge.Core.Commons.Enum;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Properties;
using System.Reactive.Subjects;
using System.Text;

namespace Elder.DataForge.Core.CodeGenerators.MessagePack
{
    public enum GenerationMode { SharedDTO, UnityDOD }

    public class MessagePackSourceEmitter : ICodeEmitter
    {
        private const string EL = "\r\n"; // Windows 표준 줄 바꿈 강제

        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();

        private string _targetDataNamespace;
        private string _targetParserNamespace;

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);
        private void WriteLine(StringBuilder sb, string text = "") => sb.Append(text).Append(EL);

        public async Task<List<GeneratedSourceCode>> GenerateAsync(List<TableSchema> schemas)
        {
            _targetDataNamespace = Settings.Default.RootNamespace;
            _targetParserNamespace = Settings.Default.RootNamespace + ".Convert";

            var generatedFiles = new List<GeneratedSourceCode>();

            if (schemas == null || !schemas.Any())
            {
                UpdateProgressLevel("No schemas found to generate.");
                UpdateProgressValue(100f);
                return generatedFiles;
            }

            UpdateProgressLevel($"Starting Source Code Generation... (Total: {schemas.Count} tables)");
            for (int i = 0; i < schemas.Count; i++)
            {
                var schema = schemas[i];
                // 진행률 계산 (0% ~ 100%)
                float progress = (float)(i + 1) / schemas.Count * 100f;

                UpdateProgressLevel($"Generating codes for Table: {schema.TableName} ({i + 1}/{schemas.Count})");

                var dtoName = $"{MemoryPackConsts.Prefix}{schema.TableName}{MemoryPackConsts.DTOSuffix}";
                var dodName = $"{MemoryPackConsts.Prefix}{schema.TableName}{MemoryPackConsts.DODSuffix}";

                generatedFiles.Add(new GeneratedSourceCode($"{dtoName}.cs", GenerateModelContent(dtoName, schema.AnalyzedFields), SourceCategory.Dto));
                generatedFiles.Add(new GeneratedSourceCode($"{dtoName}.Parser.cs", GenerateParserContent(dtoName, schema.AnalyzedFields), SourceCategory.Parser));
                generatedFiles.Add(new GeneratedSourceCode($"{dodName}.cs", GenerateRuntimeContent(dodName, schema.AnalyzedFields), SourceCategory.Dod));
                UpdateProgressValue(progress);
                await Task.Delay(1);
            }

            UpdateProgressLevel("All source codes have been generated successfully.");
            return await Task.FromResult(generatedFiles);
        }

        private string GenerateModelContent(string name, List<AnalyzedField> fields)
        {
            var sb = new StringBuilder();
            WriteLine(sb, "using System;\nusing System.Collections.Generic;\nusing MessagePack;\n");
            WriteLine(sb, $"namespace {_targetDataNamespace}\n{{");
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
            WriteLine(sb, "using System;\nusing Unity.Collections;\nusing Unity.Entities;\n");
            WriteLine(sb, $"namespace {_targetDataNamespace}\n{{");
            WriteLine(sb, "\t[Serializable]\n\t[MessagePack.MessagePackObject]");
            WriteLine(sb, $"\tpublic readonly struct {name}\n\t{{");
            
            foreach (var f in fields)
                WriteLine(sb, $"\t\t[MessagePack.Key({f.KeyIndex})] public readonly {f.UnmanagedType} {f.Name}; // Size: {f.TotalSize}");
            
            WriteLine(sb, "\n\t\t[MessagePack.SerializationConstructor]");
            sb.Append($"\t\tpublic {name}(").Append(string.Join(", ", fields.Select(f => $"{f.UnmanagedType} {f.PropertyName}"))).AppendLine(")");
            WriteLine(sb, "\t\t{");
            foreach (var f in fields)
                WriteLine(sb, $"\t\t\tthis.{f.Name} = {f.PropertyName};");

            WriteLine(sb, "\t\t}\n\t}\n}");
            return sb.ToString();
        }

        private string GenerateParserContent(string dtoName, List<AnalyzedField> fields)
        {
            var sb = new StringBuilder();
            WriteLine(sb, $"using {_targetDataNamespace};\nusing System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing MessagePack;\n");
            WriteLine(sb, $"namespace {_targetParserNamespace}\n{{");
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