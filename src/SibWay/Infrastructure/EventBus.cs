using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SibWay.Infrastructure
{
    public class EventBus
    {
        private readonly ISubject<object> _subject  = new Subject<object>();


        public IDisposable Subscrube<T>(Action<T> onNext) where T : class
        {
            return _subject
                .Where(i=> i.GetType() == typeof(T))
                .Select(o => o as T)
                .Subscribe(onNext, () => {});
        }
        
        public void Publish<T>(T val) where T : class
        {
            _subject.OnNext(val);
        }
    }
}