using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Channels
{
	internal class TransportManagerContainer
	{
		private IList<TransportManager> transportManagers;

		private TransportChannelListener listener;

		private bool closed;

		public TransportManagerContainer(TransportChannelListener listener)
		{
			this.listener = listener;
			this.transportManagers = new List<TransportManager>();
		}

		private TransportManagerContainer(TransportManagerContainer source)
		{
			this.listener = source.listener;
			this.transportManagers = new List<TransportManager>();
			for (int i = 0; i < source.transportManagers.Count; i++)
			{
				this.transportManagers.Add(source.transportManagers[i]);
			}
		}

		public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new TransportManagerContainer.CloseAsyncResult(timeout, this, callback, state);
		}

		public IAsyncResult BeginOpen(TimeSpan timeout, SelectTransportManagersCallback selectTransportManagerCallback, AsyncCallback callback, object state)
		{
			return new TransportManagerContainer.OpenAsyncResult(timeout, selectTransportManagerCallback, this, callback, state);
		}

		public void Close(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			if (this.closed)
			{
				return;
			}
			lock (this.listener.TransportManagerTable)
			{
				if (!this.closed)
				{
					this.closed = true;
					foreach (TransportManager transportManager in this.transportManagers)
					{
						transportManager.Close(timeoutHelper.RemainingTime(), this.listener);
					}
					this.transportManagers.Clear();
				}
			}
		}

		public void EndClose(IAsyncResult result)
		{
			TransportManagerContainer.CloseAsyncResult.End(result);
		}

		public void EndOpen(IAsyncResult result)
		{
			TransportManagerContainer.OpenAsyncResult.End(result);
		}

		public void Open(TimeSpan timeout, SelectTransportManagersCallback selectTransportManagerCallback)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			lock (this.listener.TransportManagerTable)
			{
				if (!this.closed)
				{
					IList<TransportManager> transportManagers = selectTransportManagerCallback();
					if (transportManagers != null)
					{
						for (int i = 0; i < transportManagers.Count; i++)
						{
							TransportManager item = transportManagers[i];
							item.Open(timeoutHelper.RemainingTime(), this.listener);
							this.transportManagers.Add(item);
						}
					}
				}
			}
		}

		public static TransportManagerContainer TransferTransportManagers(TransportManagerContainer source)
		{
			TransportManagerContainer transportManagerContainer = null;
			lock (source.listener.TransportManagerTable)
			{
				if (source.transportManagers.Count > 0)
				{
					transportManagerContainer = new TransportManagerContainer(source);
					source.transportManagers.Clear();
				}
			}
			return transportManagerContainer;
		}

		private sealed class CloseAsyncResult : TransportManagerContainer.OpenOrCloseAsyncResult
		{
			private TimeSpan timeout;

			public CloseAsyncResult(TimeSpan timeout, TransportManagerContainer parent, AsyncCallback callback, object state) : base(parent, callback, state)
			{
				this.timeout = timeout;
				base.Begin();
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<TransportManagerContainer.CloseAsyncResult>(result);
			}

			protected override void OnScheduled(TransportManagerContainer parent)
			{
				parent.Close(this.timeout);
			}
		}

		private sealed class OpenAsyncResult : TransportManagerContainer.OpenOrCloseAsyncResult
		{
			private SelectTransportManagersCallback selectTransportManagerCallback;

			private TimeSpan timeout;

			public OpenAsyncResult(TimeSpan timeout, SelectTransportManagersCallback selectTransportManagerCallback, TransportManagerContainer parent, AsyncCallback callback, object state) : base(parent, callback, state)
			{
				this.timeout = timeout;
				this.selectTransportManagerCallback = selectTransportManagerCallback;
				base.Begin();
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<TransportManagerContainer.OpenAsyncResult>(result);
			}

			protected override void OnScheduled(TransportManagerContainer parent)
			{
				parent.Open(this.timeout, this.selectTransportManagerCallback);
			}
		}

		private abstract class OpenOrCloseAsyncResult : AsyncResult
		{
			private TransportManagerContainer parent;

			private readonly static Action<object> scheduledCallback;

			static OpenOrCloseAsyncResult()
			{
				TransportManagerContainer.OpenOrCloseAsyncResult.scheduledCallback = new Action<object>(TransportManagerContainer.OpenOrCloseAsyncResult.OnScheduled);
			}

			protected OpenOrCloseAsyncResult(TransportManagerContainer parent, AsyncCallback callback, object state) : base(callback, state)
			{
				this.parent = parent;
			}

			protected void Begin()
			{
				IOThreadScheduler.ScheduleCallbackNoFlow(TransportManagerContainer.OpenOrCloseAsyncResult.scheduledCallback, this);
			}

			private static void OnScheduled(object state)
			{
				((TransportManagerContainer.OpenOrCloseAsyncResult)state).OnScheduled();
			}

			private void OnScheduled()
			{
				using (Activity activity = ServiceModelActivity.BoundOperation(null))
				{
					Exception exception = null;
					try
					{
						this.OnScheduled(this.parent);
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						exception = exception1;
					}
					base.Complete(false, exception);
				}
			}

			protected abstract void OnScheduled(TransportManagerContainer parent);
		}
	}
}