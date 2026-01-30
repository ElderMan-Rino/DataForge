using Elder.DataForge.Core.CodeGenerators.MessagePack;
using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Interfaces
{
    public interface ISourceCodeGenerator : IProgressNotifier
    {
        public Task<bool> GenerateSourceCodeAsync(IReadOnlyList<DocumentInfoData> documentInfos);
    }
}
