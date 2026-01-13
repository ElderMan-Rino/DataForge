using Elder.DataForge.Core.Interfaces;
using System.Reactive.Subjects;

namespace Elder.DataForge.Core.Notifiers
{
    public class ProgressReporter : IProgressNotifier, IDisposable
    {
        private Subject<string> _progressLevel = new();
        private Subject<float> _progressValue = new();

        public IObservable<string> OnProgressLevelUpdated => _progressLevel;

        public IObservable<float> OnProgressValueUpdated => _progressValue;

        protected void UpdateProgressLevel(string progressLevel)
        {
            _progressLevel.OnNext(progressLevel);
        }
        protected void UpdateProgressValue(float progressValue)
        {
            _progressValue.OnNext(progressValue);
        }
        protected void DisposeSubjects()
        {
            _progressLevel.Dispose();
            _progressValue.Dispose();
        }
        public virtual void Dispose()
        {
            DisposeSubjects();
        }
    }
}
