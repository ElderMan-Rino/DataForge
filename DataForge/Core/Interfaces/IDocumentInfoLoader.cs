using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Interfaces
{
    public interface IDocumentInfoLoader : IDisposable
    {
        public bool TryLoadDocumentInfos(out IEnumerable<DocumentInfoData> documentInfoDatas);
    }
}
