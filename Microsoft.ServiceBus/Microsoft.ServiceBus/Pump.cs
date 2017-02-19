using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus
{
	internal abstract class Pump : IDisposable
	{
		private readonly object pumpCompletedLock = new object();

		private bool disposed;

		private ManualResetEvent pumpCompleted;

		internal Pump.PumpAsyncResult Caller
		{
			get;
			set;
		}

		protected bool IsClosed
		{
			get
			{
				return this.disposed;
			}
		}

		internal bool IsRunning
		{
			get;
			set;
		}

		protected Pump()
		{
			this.pumpCompleted = new ManualResetEvent(false);
		}

		public abstract IAsyncResult BeginRunPump(AsyncCallback callback, object state);

		public void Dispose()
		{
			if (!this.disposed)
			{
				this.disposed = true;
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			lock (this.pumpCompletedLock)
			{
				if (this.pumpCompleted != null)
				{
					this.pumpCompleted.Set();
					this.pumpCompleted.Close();
					this.pumpCompleted = null;
				}
			}
		}

		public static void EndRunPump(IAsyncResult asyncResult)
		{
			AsyncResult<Pump.PumpAsyncResult>.End(asyncResult);
		}

		public void RunPump()
		{
			Pump.EndRunPump(this.BeginRunPump(null, null));
		}

		protected void SetComplete()
		{
			this.SetComplete((Pump.PumpAsyncResult pumpAsyncResult) => pumpAsyncResult.SetComplete());
		}

		protected void SetComplete(Exception ex)
		{
			this.SetComplete((Pump.PumpAsyncResult pumpAsyncResult) => pumpAsyncResult.SetComplete(ex));
		}

		private void SetComplete(Action<Pump.PumpAsyncResult> callerAction)
		{
			lock (this.pumpCompletedLock)
			{
				if (this.pumpCompleted != null)
				{
					this.pumpCompleted.Set();
				}
				else
				{
					return;
				}
			}
			Pump.PumpAsyncResult caller = this.Caller;
			if (caller != null)
			{
				bool flag = false;
				lock (this.pumpCompletedLock)
				{
					if (this.Caller != null)
					{
						this.Caller = null;
						flag = true;
					}
				}
				if (flag && !caller.IsCompleted)
				{
					callerAction(caller);
				}
			}
			this.IsRunning = false;
		}

		public void WaitForCompletion()
		{
			if (this.pumpCompleted != null)
			{
				try
				{
					this.pumpCompleted.WaitOne();
				}
				catch (ObjectDisposedException objectDisposedException)
				{
				}
			}
		}

		internal class PumpAsyncResult : AsyncResult<Pump.PumpAsyncResult>
		{
			public PumpAsyncResult(AsyncCallback callback, object state) : base(callback, state)
			{
			}

			internal void SetComplete()
			{
				base.Complete(false);
			}

			internal void SetComplete(Exception ex)
			{
				base.Complete(false, ex);
			}
		}
	}
}