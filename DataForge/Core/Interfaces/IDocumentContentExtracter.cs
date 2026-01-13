using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Interfaces
{
    public interface IDocumentContentExtracter : IProgressNotifier, IDisposable
    {
        public Task<Dictionary<string, DocumentContentData>> ExtractDocumentContentDataAsync(IEnumerable<DocumentInfoData> documentInfoData);
    }
}
