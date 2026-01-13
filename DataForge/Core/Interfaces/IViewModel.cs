namespace Elder.DataForge.Core.Interfaces
{
    public interface IViewModel : IDisposable
    {
        public bool TryBindModel(IModel model);
        public void FinalizeBinding();
    }
}
