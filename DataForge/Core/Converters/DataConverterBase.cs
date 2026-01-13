using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Core.Notifiers;
using Elder.DataForge.Models.Data;
using Elder.Helpers.Commons;

namespace Elder.DataForge.Core.Converters
{
    public abstract class DataConverterBase : ProgressReporter, IDataConverter
    {
        private const string ConvertingStartText = "Data Converting Start";
        private const string ConvertingEndText = "Data Converting End";

        public async Task<ConversionResult> ConvertDataAsync(IEnumerable<DocumentContentData> documentContentData)
        {
            UpdateProgressLevel(ConvertingStartText);

            var conversionDataSet = new List<ConversionData>();
            var totalItemCount = documentContentData.Count();
            var currentItemIndex = 0;
            foreach (var contentDatum in documentContentData)
            {
                UpdateProgressLevel($"Converting : {contentDatum.Name}");
                UpdateProgressValue(CommonHelpers.ToProgress(currentItemIndex++, totalItemCount));
                conversionDataSet.Add(ConvertContent(contentDatum));
                await Task.Yield();
            }
            UpdateProgressLevel(ConvertingEndText);
            UpdateProgressValue(1f);

            return new ConversionResult(true, conversionDataSet);
        }
        protected abstract ConversionData ConvertContent(DocumentContentData contentData);
    }
}
