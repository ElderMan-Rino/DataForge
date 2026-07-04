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

            var languageSchemas = schemas.Where(s => s.IsLanguageSheet).ToList();
            var normalSchemas = schemas.Where(s => !s.IsLanguageSheet).ToList();

            // ─── 일반 시트 ────────────────────────────────────────────────────
            foreach (var schema in normalSchemas)
            {
                var dodName = $"{schema.TableName}Row";
                var dtoName = string.IsNullOrEmpty(schema.DataName)
                    ? schema.TableName : schema.DataName;

                // DTO → GameData (DLL 빌드 + MPC -i 분석 대상)
                generatedFiles.Add(new GeneratedSourceCode(
                    $"{dtoName}.cs",
                    GenerateModelContent(dtoName, schema.AnalyzedFields),
                    SourceCategory.GameData));        // ← SharedDTO → GameData

                // Row → UnityScripts
                generatedFiles.Add(new GeneratedSourceCode(
                    $"{dodName}.cs",
                    GenerateRuntimeContent(dodName, schema.AnalyzedFields),
                    SourceCategory.GameData));      // UnityScripts → GameData (DLL 포함)

                generatedFiles.Add(new GeneratedSourceCode(
                    $"{schema.TableName}Root.cs",
                    GenerateRootContent(schema.TableName, dodName),
                    SourceCategory.GameData));      // UnityScripts → GameData (DLL 포함)

                // Baker → EditorScripts
                generatedFiles.Add(new GeneratedSourceCode(
                    $"{schema.TableName}Baker.cs",
                    GenerateBakerContent(schema.TableName, dtoName, dodName,
                        schema.AnalyzedFields),
                    SourceCategory.EditorScripts));
            }

            // ─── 언어 시트 ────────────────────────────────────────────────────
            // GeneratedBlobLoader 제외: LocalizeSystem이 언어 blob 로드를 전담.
            if (languageSchemas.Any())
            {
                var languageGroups = languageSchemas
                    .GroupBy(s => s.DataName)
                    .ToList();

                foreach (var group in languageGroups)
                {
                    var dataName = group.Key;
                    var bakerName = $"{dataName}Baker";
                    var dodName = $"{dataName}Row";
                    var firstSchema = group.First();

                    // DTO → SharedDTO (Baker의 MessagePack 파싱에 필요)
                    generatedFiles.Add(new GeneratedSourceCode(
                        $"{dataName}.cs",
                        GenerateModelContent(dataName, firstSchema.AnalyzedFields),
                        SourceCategory.SharedDTO));

                    // Row → GameData
                    generatedFiles.Add(new GeneratedSourceCode(
                        $"{dodName}.cs",
                        GenerateRuntimeContent(dodName, firstSchema.AnalyzedFields),
                        SourceCategory.GameData));

                    // Root → GameData
                    generatedFiles.Add(new GeneratedSourceCode(
                        $"{dataName}Root.cs",
                        GenerateRootContent(dataName, dodName),
                        SourceCategory.GameData));

                    // Baker → EditorScripts
                    var tableNames = group.Select(s => s.TableName).ToList();
                    generatedFiles.Add(new GeneratedSourceCode(
                        $"{bakerName}.cs",
                        GenerateLocalizeBakerContent(bakerName, dataName, dodName, tableNames, firstSchema.AnalyzedFields),
                        SourceCategory.EditorScripts));
                }
            }

            return await Task.FromResult(generatedFiles);
        }
        private string GenerateLocalizeBakerContent(
            string bakerName,          // "LocaleEntryBaker"
            string dtoName,            // "LocaleEntry"
            string dodName,            // "LocaleEntryRow"
            List<string> tableNames,   // ["ErrorMsgLocale_Ko", "ErrorMsgLocale_En", ...]
            List<AnalyzedField> fields)
        {
            string rootTypeName = $"{dtoName}Root";

            var sb = new StringBuilder();
            WriteLine(sb, "#if UNITY_EDITOR");
            WriteLine(sb, "using MessagePack;");
            WriteLine(sb, "using MessagePack.Resolvers;");
            WriteLine(sb, "using System.Collections.Generic;");
            WriteLine(sb, "using System.IO;");
            WriteLine(sb, "using System.Linq;");
            WriteLine(sb, "using Unity.Collections;");
            WriteLine(sb, "using Unity.Entities;");
            WriteLine(sb, "using Unity.Entities.Serialization;");
            WriteLine(sb, "");
            WriteLine(sb, $"namespace {_targetDataNamespace}");
            WriteLine(sb, "{");
            WriteLine(sb, $"\tpublic static class {bakerName}");
            WriteLine(sb, "\t{");

            // ─── ParseDto ────────────────────────────────────────────────────
            var orderedByKey = fields.OrderBy(f => f.KeyIndex).ToList();
            var ctorArgList = string.Join(", ", orderedByKey.Select(f =>
            {
                string baseType = f.ManagedType.Replace("List<", "").Replace(">", "");
                if (f.IsList)
                {
                    string elemConvert = baseType switch
                    {
                        "int"    => "System.Convert.ToInt32(x)",
                        "long"   => "System.Convert.ToInt64(x)",
                        "float"  => "System.Convert.ToSingle(x)",
                        "bool"   => "System.Convert.ToBoolean(x)",
                        "string" => "x?.ToString() ?? string.Empty",
                        _        => $"({baseType})System.Convert.ToInt32(x)"
                    };
                    return $"((System.Collections.IEnumerable)row[{f.KeyIndex}]).Cast<object>().Select(x => {elemConvert}).ToList()";
                }
                return baseType switch
                {
                    "int"    => $"System.Convert.ToInt32(row[{f.KeyIndex}])",
                    "long"   => $"System.Convert.ToInt64(row[{f.KeyIndex}])",
                    "float"  => $"System.Convert.ToSingle(row[{f.KeyIndex}])",
                    "bool"   => $"System.Convert.ToBoolean(row[{f.KeyIndex}])",
                    "string" => $"row[{f.KeyIndex}]?.ToString() ?? string.Empty",
                    _        => $"({baseType})System.Convert.ToInt32(row[{f.KeyIndex}])"
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

            // ─── SaveBlob ─────────────────────────────────────────────────────
            WriteLine(sb, $"\t\tprivate static void SaveBlob(");
            WriteLine(sb, $"\t\t\tBlobAssetReference<{rootTypeName}> blobRef,");
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

            // ─── BakeInternal ────────────────────────────────────────────────
            WriteLine(sb, "\t\tprivate static void BakeInternal(");
            WriteLine(sb, "\t\t\tstring sourcePath, string savePath, byte[] encryptionKeyPartB)");
            WriteLine(sb, "\t\t{");
            WriteLine(sb, "\t\t\tvar dtoList = ParseDto(sourcePath);");
            WriteLine(sb, $"\t\t\tvar builder = new BlobBuilder(Allocator.Temp);");
            WriteLine(sb, $"\t\t\tref {rootTypeName} root = ref builder.ConstructRoot<{rootTypeName}>();");
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
            WriteLine(sb, $"\t\t\tvar blobRef = builder.CreateBlobAssetReference<{rootTypeName}>(Allocator.Temp);");
            WriteLine(sb, "\t\t\tbuilder.Dispose();");
            WriteLine(sb, "\t\t\tSaveBlob(blobRef, savePath, encryptionKeyPartB);");
            WriteLine(sb, "\t\t}");
            WriteLine(sb, "");

            // ─── 시트별 진입점 (tableName당 1개) ─────────────────────────────
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
            WriteLine(sb, "using System.Collections.Generic;");
            WriteLine(sb, "");
            WriteLine(sb, $"namespace {_targetDataNamespace}");
            WriteLine(sb, "{");
            WriteLine(sb, "\tpublic sealed class GeneratedBlobLoader : IGameDataLoader");
            WriteLine(sb, "\t{");
            WriteLine(sb, "\t\tpublic UniTask LoadAsync(IDataSheetLoader sheetLoader, int hash, int scope)");
            WriteLine(sb, "\t\t{");
            WriteLine(sb, "\t\t\treturn GeneratedBlobRegistry.Registry.TryGetValue(hash, out var load)");
            WriteLine(sb, "\t\t\t\t? load(sheetLoader, scope)");
            WriteLine(sb, "\t\t\t\t: throw new KeyNotFoundException(hash.ToString()); // [HEAP] error path only");
            WriteLine(sb, "\t\t}");
            WriteLine(sb, "\t}");
            WriteLine(sb, "}");
            return sb.ToString();
        }

        public string GenerateBlobRegistryContent(List<SheetEntry> activeSheets)
        {
            _targetDataNamespace = Settings.Default.RootNamespace;

            var normalSheets   = activeSheets.Where(s => !s.IsLanguageSheet).ToList();
            var languageSheets = activeSheets.Where(s =>  s.IsLanguageSheet).ToList();

            var sb = new StringBuilder();
            WriteLine(sb, "using Cysharp.Threading.Tasks;");
            WriteLine(sb, "using Elder.Framework.Data.Interfaces;");
            WriteLine(sb, "using System;");
            WriteLine(sb, "using System.Collections.Generic;");
            WriteLine(sb, "");
            WriteLine(sb, $"namespace {_targetDataNamespace}");
            WriteLine(sb, "{");
            WriteLine(sb, "\tpublic static class GeneratedBlobRegistry");
            WriteLine(sb, "\t{");
            WriteLine(sb, "\t\t// [HEAP] 초기화 시 1회 할당");
            WriteLine(sb, "\t\tpublic static readonly Dictionary<int, Func<IDataSheetLoader, int, UniTask>> Registry = new()");
            WriteLine(sb, "\t\t{");
            foreach (var sheet in normalSheets)
                WriteLine(sb, $"\t\t\t[SheetKey.{sheet.TableName}Hash] = static (l, scope) => l.LoadSheetAsync<{sheet.TableName}Root>(SheetKey.{sheet.TableName}, scope),");
            foreach (var sheet in languageSheets)
                WriteLine(sb, $"\t\t\t[SheetKey.{sheet.TableName}Hash] = static (l, scope) => l.LoadSheetAsync<{sheet.DataName}Root>(SheetKey.{sheet.TableName}, scope),");
            WriteLine(sb, "\t\t};");
            WriteLine(sb, "\t}");
            WriteLine(sb, "}");
            return sb.ToString();
        }

        public string GenerateSheetKeyContent(List<SheetEntry> activeSheets)
        {
            _targetDataNamespace = Settings.Default.RootNamespace;

            var keys = activeSheets
                .Select(s => s.TableName)
                .ToList();

            int maxLen     = keys.Max(k => k.Length);
            int maxHashLen = keys.Max(k => (k + "Hash").Length);

            var sb = new StringBuilder();
            WriteLine(sb, "using Elder.Framework.Common.Utils;");
            WriteLine(sb, "");
            WriteLine(sb, $"namespace {_targetDataNamespace}");
            WriteLine(sb, "{");
            WriteLine(sb, "\tpublic static class SheetKey");
            WriteLine(sb, "\t{");
            foreach (var key in keys)
            {
                WriteLine(sb, $"\t\tpublic const string          {key.PadRight(maxLen)}     = \"{key}\";");
                WriteLine(sb, $"\t\tpublic static readonly int   {(key + "Hash").PadRight(maxHashLen)} = StringHashHelper.ToStableHash({key});");
            }
            WriteLine(sb, "\t}");
            WriteLine(sb, "}");
            return sb.ToString();
        }

        private string GenerateModelContent(string name, List<AnalyzedField> fields)
        {
            _targetDataNamespace = Settings.Default.RootNamespace;

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
            _targetDataNamespace = Settings.Default.RootNamespace;

            var sb = new StringBuilder();
            WriteLine(sb, "using System;\nusing Unity.Burst;\nusing Unity.Entities;\n");
            WriteLine(sb, $"namespace {_targetDataNamespace}\n{{");
            WriteLine(sb, "\t[BurstCompile]");
            WriteLine(sb, $"\tpublic struct {name}\n\t{{");

            foreach (var f in fields.OrderByDescending(f => f.TotalSize))
                WriteLine(sb, $"\t\tpublic {f.UnmanagedType} {f.Name};");

            WriteLine(sb, "\t}\n}");
            return sb.ToString();
        }

        private string GenerateRootContent(string tableName, string dodName)
        {
            _targetDataNamespace = Settings.Default.RootNamespace;

            var sb = new StringBuilder();
            WriteLine(sb, "using Unity.Burst;");
            WriteLine(sb, "using Unity.Entities;");
            WriteLine(sb, $"namespace {_targetDataNamespace}\n{{");
            WriteLine(sb, "\t[BurstCompile]");
            WriteLine(sb, $"\tpublic struct {tableName}Root\n\t{{");
            WriteLine(sb, $"\t\tpublic BlobArray<{dodName}> Rows;");
            WriteLine(sb, "\t}\n}");
            return sb.ToString();
        }

        private string GenerateBakerContent(string tableName, string dtoName, string dodName, List<AnalyzedField> fields)
        {
            _targetDataNamespace = Settings.Default.RootNamespace;

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
                {
                    string elemConvert = baseType switch
                    {
                        "int"    => "System.Convert.ToInt32(x)",
                        "long"   => "System.Convert.ToInt64(x)",
                        "float"  => "System.Convert.ToSingle(x)",
                        "bool"   => "System.Convert.ToBoolean(x)",
                        "string" => "x?.ToString() ?? string.Empty",
                        _        => $"({baseType})System.Convert.ToInt32(x)"
                    };
                    return $"((System.Collections.IEnumerable)row[{f.KeyIndex}]).Cast<object>().Select(x => {elemConvert}).ToList()";
                }
                return baseType switch
                {
                    "int"    => $"System.Convert.ToInt32(row[{f.KeyIndex}])",
                    "long"   => $"System.Convert.ToInt64(row[{f.KeyIndex}])",
                    "float"  => $"System.Convert.ToSingle(row[{f.KeyIndex}])",
                    "bool"   => $"System.Convert.ToBoolean(row[{f.KeyIndex}])",
                    "string" => $"row[{f.KeyIndex}]?.ToString() ?? string.Empty",
                    _        => $"({baseType})System.Convert.ToInt32(row[{f.KeyIndex}])"
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