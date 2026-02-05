using Elder.DataForge.Core.CodeGenerators;
using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Interfaces
{
    public interface ICodeEmitter : IProgressNotifier
    {
        public Task<List<GeneratedSourceCode>> GenerateAsync(List<TableSchema> schemas);
    }
}
