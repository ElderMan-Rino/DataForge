using Elder.DataForge.Core.CodeGenerators.MessagePack;
using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Interfaces
{
    public interface ISourceCodeGenerator
    {
        Task<List<GeneratedSourceCode>> GenerateAsync(List<TableSchema> schemas);
    }
}
