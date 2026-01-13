namespace Elder.DataForge.Core.Interfaces
{
    public interface IDataExporter : IProgressNotifier, IDisposable
    {
        public Task<bool> TryExportDataAsync();
    }
}
