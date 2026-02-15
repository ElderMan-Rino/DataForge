using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Microsoft.Win32;
using System.Reactive.Linq;

namespace Elder.DataForge.Core.InfoLoader
{
    public abstract class DocumentInfoLoaderBase : IDocumentInfoLoader
    {
        protected abstract string _dialogTitle { get; }
        protected abstract string _filter { get; }
        protected abstract string _loadErrorMsg { get; }

        public bool TryLoadDocumentInfos(out IEnumerable<DocumentInfoData> documentInfoDataSet)
        {
            return HandleFileSelection(CreateOpenFileDialog(), out documentInfoDataSet);
        }
        private OpenFileDialog CreateOpenFileDialog()
        {
            return new OpenFileDialog()
            {
                Title = _dialogTitle,
                Filter = _filter,
                Multiselect = true,
            };
        }
        private bool HandleFileSelection(OpenFileDialog dialog, out IEnumerable<DocumentInfoData> documentInfoDataSet)
        {
            documentInfoDataSet = null;
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    documentInfoDataSet = dialog.FileNames.Select(CreateDocumentData);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{_loadErrorMsg}: {ex.Message}");
                }
            }

            return false;
        }
        public abstract DocumentInfoData CreateDocumentData(string filePath);
        public virtual void Dispose()
        {
            
        }
    }
}
