namespace Elder.DataForge.Core.Interfaces
{
    public interface IProgressNotifier
    {
        public IObservable<string> OnProgressLevelUpdated { get; }
        public IObservable<float> OnProgressValueUpdated { get; }
    }
}
