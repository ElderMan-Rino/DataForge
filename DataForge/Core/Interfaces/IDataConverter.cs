using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Interfaces
{
    public interface IDataConverter : IProgressNotifier, IDisposable
    {
       
        public Task<ConversionResult> ConvertDataAsync(IEnumerable<DocumentContentData> documentContentData);
    }
   
}
