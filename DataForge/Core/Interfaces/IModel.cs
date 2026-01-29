using Elder.DataForge.Models.Data;
using System.Collections.ObjectModel;

namespace Elder.DataForge.Core.Interfaces
{
    public interface IModel 
    {
        public ObservableCollection<DocumentInfoData> DocumenttInfoDataCollection { get; }
    }
}
