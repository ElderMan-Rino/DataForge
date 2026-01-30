using Elder.DataForge.Models.Data;
using System.Collections.ObjectModel;

namespace Elder.DataForge.Core.Interfaces
{
    internal interface IDocumentReader : IProgressNotifier
    {
        public ObservableCollection<DocumentInfoData> DocumenttInfoDataCollection { get; }
        public Task<bool> ReadDocumentProcessAsync();
    }
}
