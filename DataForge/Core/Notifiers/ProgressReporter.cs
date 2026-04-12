using Elder.DataForge.Core.Interfaces;
using System.Reactive.Subjects;

namespace Elder.DataForge.Core.Notifiers
{
    public class ProgressReporter : IProgressNotifier, IDisposable
    {
        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();
        private Subject<string> _updateOutputLog = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        public IObservable<string> OnOutputLogUpdated => _updateOutputLog;

        protected void UpdateProgressLevel(string progressLevel)
        {
            _updateProgressLevel.OnNext(progressLevel);
        }
        protected void UpdateProgressValue(float progressValue)
        {
            _updateProgressValue.OnNext(progressValue);
        }
        protected void DisposeSubjects()
        {
            _updateProgressLevel.Dispose();
            _updateProgressValue.Dispose();
        }
        public virtual void Dispose()
        {
            DisposeSubjects();
        }
    }
}
