using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Core.Notifiers;
using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.ContentExtracters
{
    public abstract class DocumentContentExtracterBase : ProgressReporter, IDocumentContentExtracter
    {
        private const string ExtractingStartText = "Data Extracting Start";
        private const string ExtractingEndText = "Data Extracting End";
        private const string ExtractingProgressText = "Extracting : ";

        public async Task<Dictionary<string, DocumentContentData>> ExtractDocumentContentDataAsync(IEnumerable<DocumentInfoData> documentInfoData)
        {
            UpdateProgressLevel(ExtractingStartText);
            UpdateProgressValue(0f);
            var currentCount = 0;
            var totalLength = documentInfoData.Count();
            var extractedData = new Dictionary<string, DocumentContentData>();
            foreach (var info in documentInfoData)
            {
                var contentData = ExtractDocumentContents(info);
                if (contentData != null)
                {
                    extractedData.Add(contentData.Name, contentData);
                    UpdateProgressLevel($"{ExtractingProgressText} {contentData.Name}");
                }
                await Task.Yield();

                var currentProgress = (float)++currentCount / (float)totalLength;
                UpdateProgressValue(currentProgress);
            }
            UpdateProgressLevel(ExtractingEndText);
            return extractedData;
        }
        protected abstract DocumentContentData ExtractDocumentContents(DocumentInfoData documentInfo);
    }
}
