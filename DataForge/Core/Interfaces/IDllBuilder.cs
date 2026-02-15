namespace Elder.DataForge.Core.Interfaces
{
    public interface IDllBuilder : IProgressNotifier
    {
        public Task<bool> BuildDllAsync();
    }
}
