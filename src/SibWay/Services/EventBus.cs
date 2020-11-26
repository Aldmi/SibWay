using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SibWay.Services
{
    public class EventBus
    {
        private ISubject<object> Subject { get;} = new Subject<object>();


        public IDisposable Subscrube<T>(Action<T> onNext) where T : class
        {
            return Subject
                .Where(i=> i.GetType() == typeof(T))
                .Select(o => o as T)
                .Subscribe(onNext, () => {});
        }
        
        public void Publish<T>(T val) where T : class
        {
            Subject.OnNext(val);
        }
    }
}