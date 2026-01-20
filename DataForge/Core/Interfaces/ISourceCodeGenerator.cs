using Elder.DataForge.Core.CodeGenerators.MessagePack;
using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Interfaces
{
    public interface ISourceCodeGenerator : IDisposable
    {
        public Task<List<GeneratedSourceCode>> GenerateAsync(Dictionary<string, DocumentContentData> documentContents);
    }
}
