using Elder.DataForge.Core.CodeGenerator;

namespace Elder.DataForge.Core.Interfaces
{
    public interface ISourceCodeSaver : IProgressNotifier
    {
        public Task<bool> ExportAsync(List<GeneratedSourceCode> generatedSources);
    }
}
