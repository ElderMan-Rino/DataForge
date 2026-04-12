using Elder.DataForge.Core.Common.Const.MessagePack;
using Elder.DataForge.Core.Commons.Enum;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Elder.DataForge.Core.CodeGenerator.MessagePack
{
    public enum GenerationMode { SharedDTO, UnityDOD }

    public class MessagePackSourceEmitter : ICodeEmitter
    {
        private const string EL = "\r\n";

        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();
        private Subject<string> _updateOutputLog = new();

        private string _targetDataNamespace;
        private string _targetParserNamespace;

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        public IObservable<string> OnOutputLogUpdated => _updateOutputLog;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);
        private void UpdateOutputLog(string outputLog) => _updateOutputLog.OnNext(outputLog);

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
                float progress = (float)(i + 1) / schemas.Count * 100f;

                UpdateProgressLevel($"Generating codes for Table: {schema.TableName} ({i + 1}/{schemas.Count})");

                var dtoName = $"{MessagePackConsts.Prefix}{schema.TableName}{MessagePackConsts.DTOSuffix}";
                var dodName = $"{MessagePackConsts.Prefix}{schema.TableName}{MessagePackConsts.DODSuffix}";

                generatedFiles.Add(new GeneratedSourceCode($"{dtoName}.cs", GenerateModelContent(dtoName, schema.AnalyzedFields), SourceCategory.EditorData));
                generatedFiles.Add(new GeneratedSourceCode($"{dtoName}.Parser.cs", GenerateParserContent(dtoName, schema.AnalyzedFields), SourceCategory.Parser));

                // 유니티용 DOD 스크립트 생성
                generatedFiles.Add(new GeneratedSourceCode($"{dodName}.cs", GenerateRuntimeContent(dodName, schema.AnalyzedFields), SourceCategory.GameData));

                UpdateProgressValue(progress);
                await Task.Delay(1);
            }

            // ✨ 명시적이고 안전한 람다 기반 로더 클래스 생성
            UpdateProgressLevel("Generating AOT Safe Data Loader...");
            generatedFiles.Add(new GeneratedSourceCode("GeneratedDataLoader.cs", GenerateDataLoaderContent(schemas), SourceCategory.GameData));

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

        // ✨ 수정됨: IDataRecord 등 유니티 종속성(Interface) 완벽 제거
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
            WriteLine(sb, "\t\t}");
            WriteLine(sb, "\t}\n}");
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

        // ✨ 수정됨: 람다식(data => data.Id)을 주입하여 박싱(Boxing) 없는 완벽한 매핑 지원
        private string GenerateDataLoaderContent(List<TableSchema> schemas)
        {
            var sb = new StringBuilder();
            WriteLine(sb, "using Cysharp.Threading.Tasks;");
            WriteLine(sb, "using Elder.Framework.Data.Interfaces;");
            WriteLine(sb, "");
            WriteLine(sb, $"namespace {_targetDataNamespace}");
            WriteLine(sb, "{");
            WriteLine(sb, "\tpublic class GeneratedDataLoader : IGameDataLoader");
            WriteLine(sb, "\t{");
            WriteLine(sb, "\t\tpublic async UniTask LoadAllAsync(IDataSheetLoader loader)");
            WriteLine(sb, "\t\t{");

            foreach (var schema in schemas)
            {
                var dodName = $"{MessagePackConsts.Prefix}{schema.TableName}{MessagePackConsts.DODSuffix}";

                // 구조체에서 ID 필드를 찾습니다. (이름이 Id, id 등인 필드, 없으면 첫 번째 필드)
                var idField = schema.AnalyzedFields.FirstOrDefault(f => f.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)) ?? schema.AnalyzedFields.FirstOrDefault();
                string idFieldName = idField != null ? idField.Name : "Id";

                // 제네릭과 람다를 사용해 명시적 호출
                WriteLine(sb, $"\t\t\tawait loader.LoadSheetAsync<{dodName}>(\"{schema.TableName}\", data => (int)data.{idFieldName});");
            }

            WriteLine(sb, "\t\t}");
            WriteLine(sb, "\t}");
            WriteLine(sb, "}");
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