namespace Elder.DataForge.Core.Interfaces
{
    public interface IViewModel 
    {
        public IObservable<string> OnProgressLevelUpdated { get; }
        public IObservable<float> OnProgressValueUpdated { get; }
        public bool TryBindModel(IModel model);
        public void FinalizeBinding();
    }
}
