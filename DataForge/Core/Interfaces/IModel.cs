using Elder.DataForge.Models.Data;
using System.Collections.ObjectModel;

namespace Elder.DataForge.Core.Interfaces
{
    public interface IModel 
    {
        public ObservableCollection<DocumentInfoData> DocumenttInfoDataCollection { get; }
        public ObservableCollection<string>? LogMessages { get; }

        public IObservable<string> OnProgressLevelUpdated { get; }
        public IObservable<float> OnProgressValueUpdated { get; }
        public IObservable<string> OnOutputLogUpdated { get; }

        public void LoadDocument();
        public void GenerateSourceCodes();
        public void ExportData();
        public void BuildDll();
        public void OpenFolder();
    }
}
