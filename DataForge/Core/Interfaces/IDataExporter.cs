using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Interfaces
{
    public interface IDataExporter : IProgressNotifier
    {
        public Task<bool> ExportDataAsync(IReadOnlyList<DocumentInfoData> documentInfos);
    }
}
