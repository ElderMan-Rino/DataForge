using Elder.DataForge.Core.InfoLoader.Excel;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Data;

namespace Elder.DataForge.Core.DocumentReader.Excel
{
    public class ExcelDocumentReader : IDocumentReader
    {
        private static object _lock = new object();

        private IDocumentInfoLoader _infoLoader = new ExcelInfoLoader();

        private Dictionary<string, DocumentInfoData> _documenttInfoDataMap = new();

        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();

        public ObservableCollection<DocumentInfoData> DocumenttInfoDataCollection { get; private set; } = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        public ExcelDocumentReader()
        {
            BindingOperations.EnableCollectionSynchronization(DocumenttInfoDataCollection, _lock);
        }

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);

        public async Task<bool> ReadDocumentProcessAsync()
        {
            await ClearDocumentInfosProcessAsync();
            await ClearClearDocumentInfoCollectionProcessAsync();
            var result = await ReadDocumentAsync();
            return result;
        }

        private async Task ClearDocumentInfosProcessAsync()
        {
            UpdateProgressLevel("ClearDocumentInfos");
            ClearDocumentInfos();
            UpdateProgressValue(5f);
            await Task.Delay(5);
        }

        private async Task ClearClearDocumentInfoCollectionProcessAsync()
        {
            UpdateProgressLevel("ClearDocumentInfoCollection");
            ClearDocumentInfoCollection();
            UpdateProgressValue(10f);
            await Task.Delay(5);
        }

        private async Task<bool> ReadDocumentAsync()
        {
            UpdateProgressLevel("ReadDocumentProcess Start");
            await Task.Delay(5);
            if (!_infoLoader.TryLoadDocumentInfos(out var documentInfoDatas))
            {
                UpdateProgressLevel("TryLoadDocumentInfos Failed");
                return false;
            }

            if (documentInfoDatas == null || !documentInfoDatas.Any())
            {
                UpdateProgressLevel("TryLoadDocumentInfos Failed : documentInfoData is null or Empty");
                return false;
            }

            int totalCount = documentInfoDatas.Count();
            int currentIndex = 0;
            float startRange = 10f;
            float endRange = 100f;
            float rangeSize = endRange - startRange;

            foreach (var infoData in documentInfoDatas)
            {
                _documenttInfoDataMap.Add(infoData.Name, infoData);
                AddToCollection(infoData);

                currentIndex++;
                float currentProgress = startRange + ((float)currentIndex / totalCount * rangeSize);
                UpdateProgressValue(currentProgress);

                await Task.Delay(1);
            }
            UpdateProgressLevel("ReadDocumentProcess Done");
            return true;
        }

        private void AddToCollection(DocumentInfoData data)
        {
            lock (_lock)
            {
                DocumenttInfoDataCollection.Add(data); // 이제 어느 스레드에서든 안전합니다.
            }
        }

        private void ClearDocumentInfoCollection()
        {
            foreach (var documentInfo in DocumenttInfoDataCollection)
                documentInfo.Dispose();
            DocumenttInfoDataCollection.Clear();
        }

        private void ClearDocumentInfos()
        {
            foreach (var documentInfo in _documenttInfoDataMap.Values)
                documentInfo.Dispose();

            lock (_lock)
            {
                _documenttInfoDataMap.Clear();
            }
        }
    }
}
