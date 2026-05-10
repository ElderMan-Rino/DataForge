using Elder.DataForge.Models.Data;
using Elder.DataForge.Properties;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Elder.DataForge.Core.Registry
{
    public class SheetRegistryManager
    {
        private const string RegistryFileName = "sheets.json";

        private static string RegistryFilePath =>
            Path.Combine(Settings.Default.OutputPath, RegistryFileName);

        public SheetRegistry Load()
        {
            if (!File.Exists(RegistryFilePath))
                return new SheetRegistry();

            var json = File.ReadAllText(RegistryFilePath);
            return JsonSerializer.Deserialize<SheetRegistry>(json) ?? new SheetRegistry();
        }

        public void Save(SheetRegistry registry)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(registry, options);

            string directory = Path.GetDirectoryName(RegistryFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(RegistryFilePath, json);
        }

        // ← 기존 string 기반 Merge 제거, TableSchema 기반으로 교체
        public SheetRegistry Merge(SheetRegistry existing, IEnumerable<TableSchema> schemas)
        {
            var schemaMap = schemas.ToDictionary(s => s.TableName, s => s);

            existing.Sheets.RemoveAll(s => !schemaMap.ContainsKey(s.TableName));

            foreach (var (tableName, schema) in schemaMap)
            {
                var entry = existing.Sheets.FirstOrDefault(s => s.TableName == tableName);
                if (entry == null)
                {
                    existing.Sheets.Add(new SheetEntry
                    {
                        TableName = tableName,
                        DataName = schema.DataName,
                        IsLanguageSheet = schema.IsLanguageSheet,
                    });
                }
                else
                {
                    // 기존 항목도 최신 정보로 갱신
                    entry.DataName = schema.DataName;
                    entry.IsLanguageSheet = schema.IsLanguageSheet;
                }
            }

            return existing;
        }

        public List<SheetEntry> GetSheets()
        {
            return Load().Sheets;
        }
    }
}