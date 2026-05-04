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

        public SheetRegistry Merge(SheetRegistry existing, IEnumerable<string> newTableNames)
        {
            var newSet = new HashSet<string>(newTableNames, StringComparer.Ordinal);

            existing.Sheets.RemoveAll(s => !newSet.Contains(s.TableName));

            foreach (var tableName in newSet)
            {
                if (!existing.Sheets.Any(s => s.TableName == tableName))
                    existing.Sheets.Add(new SheetEntry { TableName = tableName });
            }

            return existing;
        }

        public List<SheetEntry> GetSheets()
        {
            return Load().Sheets;
        }
    }
}