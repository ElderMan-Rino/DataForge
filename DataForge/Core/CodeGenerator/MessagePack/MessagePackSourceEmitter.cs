using Elder.DataForge.Core.Common.Const.MessagePack;
using Elder.DataForge.Core.Commons.Enum;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Properties;
using System.Collections.Generic;
using System.Linq;
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
                var rootName = $"{schema.TableName}Root";

                generatedFiles.Add(new GeneratedSourceCode($"{dtoName}.cs", GenerateModelContent(dtoName, schema.AnalyzedFields), SourceCategory.EditorData));
                generatedFiles.Add(new GeneratedSourceCode($"{dtoName}.Parser.cs", GenerateParserContent(dtoName, schema.AnalyzedFields), SourceCategory.Parser));
                generatedFiles.Add(new GeneratedSourceCode($"{dodName}.cs", GenerateRuntimeContent(dodName, schema.AnalyzedFields), SourceCategory.GameData));
                generatedFiles.Add(new GeneratedSourceCode($"{rootName}.cs", GenerateRootContent(schema.TableName, dodName), SourceCategory.GameData));
                generatedFiles.Add(new GeneratedSourceCode($"{schema.TableName}Baker.cs", GenerateBakerContent(schema.TableName, dtoName, dodName, schema.AnalyzedFields), SourceCategory.EditorData));

                UpdateProgressValue(progress);
                await Task.Delay(1);
            }

            UpdateProgressLevel("All source codes have been generated successfully.");
            return await Task.FromResult(generatedFiles);
        }

        public string GenerateDataLoaderContent(List<SheetEntry> activeSheets)
        {
            _targetDataNamespace = Settings.Default.RootNamespace;

            var sb = new StringBuilder();
            WriteLine(sb, "using Cysharp.Threading.Tasks;");
            WriteLine(sb, "using Elder.Framework.Data.Interfaces;");
            WriteLine(sb, "using Unity.Entities;");
            WriteLine(sb, "");
            WriteLine(sb, $"namespace {_targetDataNamespace}");
            WriteLine(sb, "{");
            WriteLine(sb, "\tpublic static class GeneratedBlobLoader");
            WriteLine(sb, "\t{");
            WriteLine(sb, "\t\tpublic static async UniTask LoadAllDataAsync(IDataSheetLoader sheetLoader)");
            WriteLine(sb, "\t\t{");

            if (activeSheets.Count == 1)
            {
                string rootTypeName = $"{activeSheets[0].TableName}Root";
                string addressableKey = activeSheets[0].TableName;
                WriteLine(sb, $"\t\t\tawait sheetLoader.LoadSheetAsync<{rootTypeName}>(\"{addressableKey}\");");
            }
            else
            {
                WriteLine(sb, "\t\t\tawait UniTask.WhenAll(");
                for (int i = 0; i < activeSheets.Count; i++)
                {
                    string rootTypeName = $"{activeSheets[i].TableName}Root";
                    string addressableKey = activeSheets[i].TableName;
                    string comma = i < activeSheets.Count - 1 ? "," : "";
                    WriteLine(sb, $"\t\t\t\tsheetLoader.LoadSheetAsync<{rootTypeName}>(\"{addressableKey}\"){comma}");
                }
                WriteLine(sb, "\t\t\t);");
            }

            WriteLine(sb, "\t\t}");
            WriteLine(sb, "\t}");
            WriteLine(sb, "}");
            return sb.ToString();
        }

        private string GenerateModelContent(string name, List<AnalyzedField> fields)
        {
            var sb = new StringBuilder();
            WriteLine(sb, "using System;\nusing System.Collections.Generic;\nusing MessagePack;\n");
            WriteLine(sb, $"namespace {_targetDataNamespace}\n{{");
            WriteLine(sb, "\t[MessagePackObject]");
            WriteLine(sb, $"\tpublic readonly struct {name}\n\t{{");

            foreach (var f in fields)
                WriteLine(sb, $"\t\t[Key({f.KeyIndex})] public readonly {f.ManagedType} {f.Name};");

            WriteLine(sb, "\n\t\t[SerializationConstructor]");

            var orderedFields = fields.OrderBy(f => f.KeyIndex).ToList();
            sb.Append($"\t\tpublic {name}(")
              .Append(string.Join(", ", orderedFields.Select(f => $"{f.ManagedType} {f.PropertyName}")))
              .AppendLine(")");
            WriteLine(sb, "\t\t{");
            foreach (var f in orderedFields)
                WriteLine(sb, $"\t\t\tthis.{f.Name} = {f.PropertyName};");
            WriteLine(sb, "\t\t}\n\t}\n}");
            return sb.ToString();
        }

        private string GenerateRuntimeContent(string name, List<AnalyzedField> fields)
        {
            var sb = new StringBuilder();
            WriteLine(sb, "using System;\nusing Unity.Entities;\n");
            WriteLine(sb, $"namespace {_targetDataNamespace}\n{{");
            WriteLine(sb, $"\tpublic struct {name}\n\t{{");

            foreach (var f in fields)
                WriteLine(sb, $"\t\tpublic {f.UnmanagedType} {f.Name};");

            WriteLine(sb, "\t}\n}");
            return sb.ToString();
        }

        private string GenerateRootContent(string tableName, string dodName)
        {
            var sb = new StringBuilder();
            WriteLine(sb, "using Unity.Entities;");
            WriteLine(sb, $"namespace {_targetDataNamespace}\n{{");
            WriteLine(sb, $"\tpublic struct {tableName}Root\n\t{{");
            WriteLine(sb, $"\t\tpublic BlobArray<{dodName}> Rows;");
            WriteLine(sb, "\t}\n}");
            return sb.ToString();
        }

        private string GenerateBakerContent(string tableName, string dtoName, string dodName, List<AnalyzedField> fields)
        {
            var sb = new StringBuilder();
            WriteLine(sb, "#if UNITY_EDITOR");
            WriteLine(sb, "using UnityEditor;");
            WriteLine(sb, "using System;");
            WriteLine(sb, "using System.IO;");
            WriteLine(sb, "using System.Collections.Generic;");
            WriteLine(sb, "using MessagePack;");
            WriteLine(sb, "using MessagePack.Resolvers;");
            WriteLine(sb, "using Unity.Entities;");
            WriteLine(sb, "using Unity.Collections;");
            WriteLine(sb, "using Unity.Entities.Serialization;");
            WriteLine(sb, "");
            WriteLine(sb, $"namespace {_targetDataNamespace}");
            WriteLine(sb, "{");
            WriteLine(sb, $"\tpublic static class {tableName}Baker");
            WriteLine(sb, "\t{");
            WriteLine(sb, $"\t\tpublic static void Bake(string sourcePath, string savePath)");
            WriteLine(sb, "\t\t{");
            WriteLine(sb, "\t\t\tvar rawBytes = File.ReadAllBytes(sourcePath);");
            WriteLine(sb, "\t\t\tvar resolver = MessagePack.Resolvers.CompositeResolver.Create(");
            WriteLine(sb, "\t\t\t\tMessagePack.Resolvers.DynamicEnumAsStringResolver.Instance,");
            WriteLine(sb, "\t\t\t\tMessagePack.Resolvers.StandardResolver.Instance");
            WriteLine(sb, "\t\t\t);");
            WriteLine(sb, "\t\t\tvar options = MessagePackSerializerOptions.Standard.WithResolver(resolver);");
            WriteLine(sb, $"\t\t\tvar dtoList = MessagePackSerializer.Deserialize<List<{dtoName}>>(rawBytes, options);");
            WriteLine(sb, "");
            WriteLine(sb, "\t\t\tvar builder = new BlobBuilder(Allocator.Temp);");
            WriteLine(sb, $"\t\t\tref {tableName}Root root = ref builder.ConstructRoot<{tableName}Root>();");
            WriteLine(sb, "\t\t\tvar arrayBuilder = builder.Allocate(ref root.Rows, dtoList.Count);");
            WriteLine(sb, "");
            WriteLine(sb, "\t\t\tfor (int i = 0; i < dtoList.Count; i++)");
            WriteLine(sb, "\t\t\t{");

            foreach (var f in fields)
            {
                if (f.UnmanagedType == "BlobString")
                    WriteLine(sb, $"\t\t\t\tbuilder.AllocateString(ref arrayBuilder[i].{f.Name}, dtoList[i].{f.Name});");
                else if (f.IsList)
                {
                    WriteLine(sb, $"\t\t\t\tvar {f.Name}Builder = builder.Allocate(ref arrayBuilder[i].{f.Name}, dtoList[i].{f.Name}.Count);");
                    WriteLine(sb, $"\t\t\t\tfor (int j = 0; j < dtoList[i].{f.Name}.Count; j++) {f.Name}Builder[j] = dtoList[i].{f.Name}[j];");
                }
                else
                    WriteLine(sb, $"\t\t\t\tarrayBuilder[i].{f.Name} = dtoList[i].{f.Name};");
            }

            WriteLine(sb, "\t\t\t}");
            WriteLine(sb, "");
            WriteLine(sb, $"\t\t\tvar blobRef = builder.CreateBlobAssetReference<{tableName}Root>(Allocator.Temp);");
            WriteLine(sb, "\t\t\tbuilder.Dispose();");
            WriteLine(sb, "");
            WriteLine(sb, "\t\t\tvar writer = new MemoryBinaryWriter();");
            WriteLine(sb, "\t\t\twriter.Write(blobRef);");
            WriteLine(sb, "");
            WriteLine(sb, "\t\t\tunsafe");
            WriteLine(sb, "\t\t\t{");
            WriteLine(sb, "\t\t\t\tvar span = new ReadOnlySpan<byte>(writer.Data, writer.Length);");
            WriteLine(sb, "\t\t\t\tusing var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);");
            WriteLine(sb, "\t\t\t\tfileStream.Write(span);");
            WriteLine(sb, "\t\t\t}");
            WriteLine(sb, "");
            WriteLine(sb, "\t\t\twriter.Dispose();");
            WriteLine(sb, "\t\t\tblobRef.Dispose();");
            WriteLine(sb, "\t\t}");
            WriteLine(sb, "\t}");
            WriteLine(sb, "}");
            WriteLine(sb, "#endif");
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
                    foreach (var idx in f.ExcelIndices)
                        WriteLine(sb, $"\t\t\tif (rowData.Count > {idx} && !string.IsNullOrEmpty(rowData[{idx}])) {f.PropertyName}.Add({GetParseSyntax(bType, $"rowData[{idx}]")});");
                }
                else
                {
                    WriteLine(sb, $"\t\t\tvar {f.PropertyName} = (rowData.Count > {f.ExcelIndices[0]} && !string.IsNullOrEmpty(rowData[{f.ExcelIndices[0]}])) ? {GetParseSyntax(bType, $"rowData[{f.ExcelIndices[0]}]")} : default;");
                }
            }

            sb.Append($"\n\t\t\treturn new {dtoName}(")
              .Append(string.Join(", ", fields.OrderBy(f => f.KeyIndex).Select(f => f.PropertyName)))
              .AppendLine(");");
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

        public List<GeneratedSourceCode> GenerateEnums(List<EnumSchema> enumSchemas)
        {
            _targetDataNamespace = Settings.Default.RootNamespace;

            var result = new List<GeneratedSourceCode>();
            if (enumSchemas == null || enumSchemas.Count == 0)
                return result;

            foreach (var schema in enumSchemas)
            {
                result.Add(new GeneratedSourceCode(
                    $"{schema.EnumName}.cs",
                    GenerateEnumContent(schema),
                    SourceCategory.Enums
                ));
            }

            return result;
        }

        private string GenerateEnumContent(EnumSchema schema)
        {
            var sb = new StringBuilder();

            if (schema.EnumType == EnumType.Flag)
                WriteLine(sb, "using System;");
            WriteLine(sb);
            WriteLine(sb, $"namespace {_targetDataNamespace}");
            WriteLine(sb, "{");

            if (schema.EnumType == EnumType.Flag)
                WriteLine(sb, "\t[Flags]");

            WriteLine(sb, $"\tpublic enum {schema.EnumName}");
            WriteLine(sb, "\t{");

            foreach (var entry in schema.Entries)
                WriteLine(sb, $"\t\t{entry.Name} = {entry.Value},");

            WriteLine(sb, "\t}");
            WriteLine(sb, "}");

            return sb.ToString();
        }
    }
}