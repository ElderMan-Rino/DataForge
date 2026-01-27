using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Interfaces
{
    public interface IDataExporter : IProgressNotifier, IDisposable
    {
        public Task<bool> TryExportDataAsync(List<TableSchema> schemas, string outputPath);
    }
}
