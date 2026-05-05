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
            var generatedFiles = new List<GeneratedSourceCode>();

            // ─── 언어 시트 / 일반 시트 분리 ──────────────────────────────────
            var languageSchemas = schemas.Where(s => s.IsLanguageSheet).ToList();
            var normalSchemas = schemas.Where(s => !s.IsLanguageSheet).ToList();

            // ─── 일반 시트: 기존 로직 완전 동일 ──────────────────────────────
            foreach (var schema in normalSchemas)
            {
                var dodName = $"{schema.TableName}Row";
                var dtoName = string.IsNullOrEmpty(schema.DataName)
                    ? schema.TableName : schema.DataName;

                generatedFiles.Add(new GeneratedSourceCode(
                    $"{dtoName}.cs",
                    GenerateModelContent(dtoName, schema.AnalyzedFields),
                    SourceCategory.SharedDTO));

                generatedFiles.Add(new GeneratedSourceCode(
                    $"{dodName}.cs",
                    GenerateRuntimeContent(dodName, schema.AnalyzedFields),
                    SourceCategory.UnityScripts));

                generatedFiles.Add(new GeneratedSourceCode(
                    $"{schema.TableName}Root.cs",
                    GenerateRootContent(schema.TableName, dodName),
                    SourceCategory.UnityScripts));

                generatedFiles.Add(new GeneratedSourceCode(
                    $"{schema.TableName}Baker.cs",
                    GenerateBakerContent(schema.TableName, dtoName, dodName,
                        schema.AnalyzedFields),
                    SourceCategory.EditorScripts));
            }

            // ─── 언어 시트: DataName 기준 공통 생성 ──────────────────────────
            if (languageSchemas.Any())
            {
                // DataName 기준으로 그룹핑 ("LanguageEntry" 단위)
                var languageGroups = languageSchemas
                    .GroupBy(s => s.DataName)
                    .ToList();

                foreach (var group in languageGroups)
                {
                    var dataName = group.Key;              // "LanguageEntry"
                    var dodName = $"{dataName}Row";       // "LanguageEntryRow"
                    var rootName = $"{dataName}Root";      // "LanguageEntryRoot"
                    var bakerName = $"{dataName}Baker";     // "LanguageEntryBaker"
                    var firstSchema = group.First();

                    // DTO — DataName 기준 1개만 생성
                    generatedFiles.Add(new GeneratedSourceCode(
                        $"{dataName}.cs",
                        GenerateModelContent(dataName, firstSchema.AnalyzedFields),
                        SourceCategory.SharedDTO));

                    // Row — DataName 기준 1개만 생성
                    generatedFiles.Add(new GeneratedSourceCode(
                        $"{dodName}.cs",
                        GenerateRuntimeContent(dodName, firstSchema.AnalyzedFields),
                        SourceCategory.UnityScripts));

                    // Root — DataName 기준 1개만 생성
                    generatedFiles.Add(new GeneratedSourceCode(
                        $"{rootName}.cs",
                        GenerateRootContent(dataName, dodName),
                        SourceCategory.UnityScripts));

                    // Baker — DataName 기준 1개, 모든 TableName의 Bake() 포함
                    var tableNames = group.Select(s => s.TableName).ToList();
                    // ["UI_Ko", "UI_En", "Quest_Ko", "Quest_En", ...]
                    generatedFiles.Add(new GeneratedSourceCode(
                        $"{bakerName}.cs",
                        GenerateLanguageBakerContent(
                            bakerName, dataName, dodName, rootName,
                            tableNames, firstSchema.AnalyzedFields),
                        SourceCategory.EditorScripts));
                }
            }

            return await Task.FromResult(generatedFiles);
        }
        private string GenerateLanguageBakerContent(
    string bakerName,      // "LanguageEntryBaker"
    string dtoName,        // "LanguageEntry"
    string dodName,        // "LanguageEntryRow"
    string rootName,       // "LanguageEntryRoot"
    List<string> tableNames, // ["UI_Ko", "UI_En", "Quest_Ko", ...]
    List<AnalyzedField> fields)
        {
            var sb = new StringBuilder();
            WriteLine(sb, "#if UNITY_EDITOR");
            WriteLine(sb, "using Elder.Framework.Crypto;");
            WriteLine(sb, "using MessagePack;");
            WriteLine(sb, "using MessagePack.Resolvers;");
            WriteLine(sb, "using System.Collections.Generic;");
            WriteLine(sb, "using System.IO;");
            WriteLine(sb, "using System.Linq;");
            WriteLine(sb, "using System.Runtime.InteropServices;");
            WriteLine(sb, "using Unity.Collections;");
            WriteLine(sb, "using Unity.Entities;");
            WriteLine(sb, "using Unity.Entities.Serialization;");
            WriteLine(sb, "");
            WriteLine(sb, $"namespace {_targetDataNamespace}");
            WriteLine(sb, "{");
            WriteLine(sb, $"\tpublic static class {bakerName}");
            WriteLine(sb, "\t{");

            // ─── 공통 ParseDto ────────────────────────────────────────────────
            var orderedByKey = fields.OrderBy(f => f.KeyIndex).ToList();
            var ctorArgList = string.Join(", ", orderedByKey.Select(f =>
            {
                string baseType = f.ManagedType.Replace("List<", "").Replace(">", "");
                return baseType switch
                {
                    "int" => $"System.Convert.ToInt32(row[{f.KeyIndex}])",
                    "long" => $"System.Convert.ToInt64(row[{f.KeyIndex}])",
                    "float" => $"System.Convert.ToSingle(row[{f.KeyIndex}])",
                    "bool" => $"System.Convert.ToBoolean(row[{f.KeyIndex}])",
                    "string" => $"row[{f.KeyIndex}]?.ToString() ?? string.Empty",
                    _ => $"({baseType})System.Convert.ToInt32(row[{f.KeyIndex}])"
                };
            }));

            WriteLine(sb, $"\t\tprivate static List<{dtoName}> ParseDto(string sourcePath)");
            WriteLine(sb, "\t\t{");
            WriteLine(sb, "\t\t\tvar rawBytes = File.ReadAllBytes(sourcePath);");
            WriteLine(sb, "\t\t\tvar options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);");
            WriteLine(sb, $"\t\t\tvar rawList = MessagePackSerializer.Deserialize<List<object[]>>(rawBytes, options);");
            WriteLine(sb, $"\t\t\treturn rawList.Select(row => new {dtoName}({ctorArgList})).ToList();");
            WriteLine(sb, "\t\t}");
            WriteLine(sb, "");

            // ─── 공통 SaveBlob ────────────────────────────────────────────────
            WriteLine(sb, $"\t\tprivate static void SaveBlob(");
            WriteLine(sb, $"\t\t\tBlobAssetReference<{rootName}> blobRef,");
            WriteLine(sb, "\t\t\tstring savePath,");
            WriteLine(sb, "\t\t\tbyte[] encryptionKeyPartB)");
            WriteLine(sb, "\t\t{");
            WriteLine(sb, "\t\t\tvar writer = new MemoryBinaryWriter();");
            WriteLine(sb, "\t\t\twriter.Write(blobRef);");
            WriteLine(sb, "\t\t\tunsafe");
            WriteLine(sb, "\t\t\t{");
            WriteLine(sb, "\t\t\t\tvar plainBytes = new byte[writer.Length];");
            WriteLine(sb, "\t\t\t\tSystem.Runtime.InteropServices.Marshal.Copy((System.IntPtr)writer.Data, plainBytes, 0, writer.Length);");
            WriteLine(sb, "\t\t\t\tElder.SkillTrial.Editor.Crypto.BlobEditorEncryptionHelper.WriteEncrypted(plainBytes, savePath, encryptionKeyPartB);");
            WriteLine(sb, "\t\t\t}");
            WriteLine(sb, "\t\t\twriter.Dispose();");
            WriteLine(sb, "\t\t\tblobRef.Dispose();");
            WriteLine(sb, "\t\t}");
            WriteLine(sb, "");

            // ─── 공통 BakeInternal ────────────────────────────────────────────
            WriteLine(sb, "\t\tprivate static void BakeInternal(");
            WriteLine(sb, "\t\t\tstring sourcePath, string savePath, byte[] encryptionKeyPartB)");
            WriteLine(sb, "\t\t{");
            WriteLine(sb, "\t\t\tvar dtoList = ParseDto(sourcePath);");
            WriteLine(sb, $"\t\t\tvar builder = new BlobBuilder(Allocator.Temp);");
            WriteLine(sb, $"\t\t\tref {rootName} root = ref builder.ConstructRoot<{rootName}>();");
            WriteLine(sb, "\t\t\tvar arrayBuilder = builder.Allocate(ref root.Rows, dtoList.Count);");
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
            WriteLine(sb, $"\t\t\tvar blobRef = builder.CreateBlobAssetReference<{rootName}>(Allocator.Temp);");
            WriteLine(sb, "\t\t\tbuilder.Dispose();");
            WriteLine(sb, "\t\t\tSaveBlob(blobRef, savePath, encryptionKeyPartB);");
            WriteLine(sb, "\t\t}");
            WriteLine(sb, "");

            // ─── 시트별 진입점 (tableName당 1개) ─────────────────────────────
            // MessageToBlobConverter가 "{TableName}Baker.Bake()" 규칙으로 탐색하므로
            // 각 TableName에 대한 얇은 래퍼 클래스를 생성
            foreach (var tableName in tableNames)
            {
                WriteLine(sb, $"\t\t// {tableName} 시트 진입점");
                WriteLine(sb, $"\t\tpublic static void Bake_{tableName}(");
                WriteLine(sb, "\t\t\tstring sourcePath, string savePath, byte[] encryptionKeyPartB)");
                WriteLine(sb, "\t\t\t=> BakeInternal(sourcePath, savePath, encryptionKeyPartB);");
                WriteLine(sb, "");
            }

            WriteLine(sb, "\t}");
            WriteLine(sb, "}");
            WriteLine(sb, "#endif");
            return sb.ToString();
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
            WriteLine(sb, "\tpublic sealed class GeneratedBlobLoader : IGameDataLoader");
            WriteLine(sb, "\t{");
            WriteLine(sb, "\t\tpublic async UniTask LoadAllAsync(IDataSheetLoader sheetLoader)");
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
            WriteLine(sb, "");
            WriteLine(sb, "\t\tpublic async UniTask LoadAsync<T>(IDataSheetLoader sheetLoader, string key) where T : unmanaged");
            WriteLine(sb, "\t\t{");
            WriteLine(sb, "\t\t\tawait sheetLoader.LoadSheetAsync<T>(key);");
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

            foreach (var f in fields.OrderByDescending(f => f.TotalSize))
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
            WriteLine(sb, "using Elder.Framework.Crypto;");
            WriteLine(sb, "using MessagePack;");
            WriteLine(sb, "using MessagePack.Resolvers;");
            WriteLine(sb, "using System.Collections.Generic;");
            WriteLine(sb, "using System.IO;");
            WriteLine(sb, "using System.Linq;");
            WriteLine(sb, "using System.Runtime.InteropServices;");
            WriteLine(sb, "using Unity.Collections;");
            WriteLine(sb, "using Unity.Entities;");
            WriteLine(sb, "using Unity.Entities.Serialization;");
            WriteLine(sb, "");
            WriteLine(sb, $"namespace {_targetDataNamespace}");
            WriteLine(sb, "{");
            WriteLine(sb, $"\tpublic static class {tableName}Baker");
            WriteLine(sb, "\t{");
            WriteLine(sb, $"\t\tpublic static void Bake(string sourcePath, string savePath, byte[] encryptionKeyPartB)");
            WriteLine(sb, "\t\t{");
            WriteLine(sb, "\t\t\tvar rawBytes = File.ReadAllBytes(sourcePath);");
            WriteLine(sb, "\t\t\tvar options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);");
            WriteLine(sb, $"\t\t\tvar rawList = MessagePackSerializer.Deserialize<List<object[]>>(rawBytes, options);");

            var orderedByKey = fields.OrderBy(f => f.KeyIndex).ToList();
            var ctorArgList = string.Join(", ", orderedByKey.Select(f =>
            {
                string baseType = f.ManagedType.Replace("List<", "").Replace(">", "");
                if (f.IsList)
                    return $"((System.Collections.IEnumerable)row[{f.KeyIndex}]).Cast<object>().Select(x => ({baseType})System.Convert.ChangeType(x, typeof({baseType}))).ToList()";
                return baseType switch
                {
                    "int" => $"System.Convert.ToInt32(row[{f.KeyIndex}])",
                    "long" => $"System.Convert.ToInt64(row[{f.KeyIndex}])",
                    "float" => $"System.Convert.ToSingle(row[{f.KeyIndex}])",
                    "bool" => $"System.Convert.ToBoolean(row[{f.KeyIndex}])",
                    "string" => $"row[{f.KeyIndex}]?.ToString() ?? string.Empty",
                    _ => $"({baseType})System.Convert.ToInt32(row[{f.KeyIndex}])"
                };
            }));
            WriteLine(sb, $"\t\t\tvar dtoList = rawList.Select(row => new {dtoName}({ctorArgList})).ToList();");
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
            WriteLine(sb, "\t\t\t\tvar plainBytes = new byte[writer.Length];");
            WriteLine(sb, "\t\t\t\tMarshal.Copy((System.IntPtr)writer.Data, plainBytes, 0, writer.Length);");
            WriteLine(sb, $"\t\t\t\tElder.SkillTrial.Editor.Crypto.BlobEditorEncryptionHelper.WriteEncrypted(plainBytes, savePath, encryptionKeyPartB);");
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
            {
                if (!string.IsNullOrEmpty(entry.Desc))
                    WriteLine(sb, $"\t\t/// <summary>{entry.Desc}</summary>");

                WriteLine(sb, $"\t\t{entry.Name} = {entry.Value},");
            }

            WriteLine(sb, "\t}");
            WriteLine(sb, "}");

            return sb.ToString();
        }
    }
}