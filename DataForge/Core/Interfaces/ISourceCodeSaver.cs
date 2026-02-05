using Elder.DataForge.Core.CodeGenerators;

namespace Elder.DataForge.Core.Interfaces
{
    public interface ISourceCodeSaver
    {
        public Task<bool> ExportAsync(List<GeneratedSourceCode> generatedSources);
    }
}
