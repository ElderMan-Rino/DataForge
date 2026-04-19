using Elder.DataForge.Models.Data;
using Elder.DataForge.Properties;
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
            foreach (var tableName in newTableNames)
            {
                var existingEntry = existing.Sheets
                    .FirstOrDefault(s => s.TableName == tableName);

                if (existingEntry == null)
                {
                    existing.Sheets.Add(new SheetEntry
                    {
                        TableName = tableName,
                        IsActive = true
                    });
                }
            }

            return existing;
        }

        public List<SheetEntry> GetActiveSheets()
        {
            return Load().Sheets.Where(s => s.IsActive).ToList();
        }
    }
}