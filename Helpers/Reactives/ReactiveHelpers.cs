namespace Elder.Reactives.Helpers
{
    public static class ReactiveHelpers
    {
        public static void Add(this IDisposable disposable, ICollection<IDisposable> disposables)
        {
            disposables.Add(disposable);
        }
    }
}
