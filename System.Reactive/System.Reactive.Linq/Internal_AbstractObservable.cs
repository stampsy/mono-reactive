using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;

namespace System.Reactive.Linq
{
	internal abstract class AbstractObservable<T> : IObservable<T>
	{
		List<IObserver<T>> observers = new List<IObserver<T>> ();
		
		bool disposed;

		public IEnumerable<IObserver<T>> Observers {
			get { return observers; }
		}

		public virtual void Dispose ()
		{
			disposed = true;
		}
		
		void CheckDisposed ()
		{
			if (disposed)
				throw new ObjectDisposedException ("observable");
		}
		
		public virtual IDisposable Subscribe (IObserver<T> observer)
		{
			CheckDisposed ();
			observers.Add (observer);
			return Disposable.Create (() => observers.Remove (observer));
		}
	}
	
	internal class HotObservable<T> : IObservable<T>
	{
		IDisposable scheduler_disposable;
		ReplaySubject<T> subject;

		public ReplaySubject<T> Subject {
			get { return subject; }
		}
		
		public HotObservable (Action<ReplaySubject<T>> work, IScheduler scheduler)
		{
			subject = new ReplaySubject<T> (scheduler);
			scheduler_disposable = scheduler.Schedule (() => work (subject));
		}
		
		public void Dispose ()
		{
			if (scheduler_disposable != null)
				scheduler_disposable.Dispose ();
			var dis = Subject as IDisposable;
			if (dis != null)
				dis.Dispose ();
		}

		public IDisposable Subscribe (IObserver<T> observer)
		{
			return Subject.Subscribe (observer);
		}
	}
	
	/*
	internal class TimerObservable : AbstractObservable<long>
	{
		IScheduler scheduler;

		public TimerObservable (DateTimeOffset dueTime, IScheduler scheduler)
		{
			if (scheduler == null)
				throw new ArgumentNullException ("scheduler");
			this.scheduler = scheduler;
		}
		
		public IScheduler Scheduler {
			get { return scheduler; }
		}
	}
	*/
	
	internal class ColdObservable<T> : IObservable<T>
	{
		IScheduler scheduler;
		Action<ISubject<T>> work;
		
		public ColdObservable (Action<ISubject<T>> work, IScheduler scheduler)
		{
			this.work = work;
			this.scheduler = scheduler;
		}

		public IDisposable Subscribe (IObserver<T> observer)
		{
			var sub = new Subject<T> ();
			sub.Subscribe (observer);
			scheduler.Schedule (() => work (sub));
			return Disposable.Create (() => {
				sub.Dispose ();
			});
		}
		
		public void Dispose ()
		{
		}
	}
}