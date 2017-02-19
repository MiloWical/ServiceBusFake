using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class CommunicationObjectManager
	{
		private readonly EventHandler onInnerObjectClosed;

		private readonly EventHandler onInnerObjectFaulted;

		private readonly HashSet<ICommunicationObject> communicationObjects;

		private bool closed;

		private bool faulted;

		private object ThisLock
		{
			get
			{
				return this.communicationObjects;
			}
		}

		public CommunicationObjectManager()
		{
			this.communicationObjects = new HashSet<ICommunicationObject>();
			this.onInnerObjectClosed = new EventHandler(this.OnInnerObjectClosed);
			this.onInnerObjectFaulted = new EventHandler(this.OnInnerObjectFaulted);
		}

		public void Abort()
		{
			IEnumerable<ICommunicationObject> communicationObjects = null;
			lock (this.ThisLock)
			{
				if (!this.closed)
				{
					this.closed = true;
					communicationObjects = new HashSet<ICommunicationObject>(this.communicationObjects);
					this.communicationObjects.Clear();
				}
			}
			if (communicationObjects != null)
			{
				foreach (ICommunicationObject communicationObject in communicationObjects)
				{
					communicationObject.Abort();
				}
			}
		}

		public void Add(ICommunicationObject communicationObject, bool faultAll = true)
		{
			bool flag = false;
			lock (this.ThisLock)
			{
				if (!this.closed)
				{
					this.communicationObjects.Add(communicationObject);
					if (faultAll)
					{
						communicationObject.SafeAddFaulted(this.onInnerObjectFaulted);
					}
					communicationObject.SafeAddClosed(this.onInnerObjectClosed);
				}
				else
				{
					flag = true;
				}
			}
			if (flag)
			{
				communicationObject.Abort();
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsWarning(new ObjectDisposedException(this.GetType().Name), null);
			}
		}

		public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			HashSet<ICommunicationObject> communicationObjects;
			lock (this.ThisLock)
			{
				if (this.closed)
				{
					communicationObjects = new HashSet<ICommunicationObject>();
				}
				else
				{
					this.closed = true;
					communicationObjects = new HashSet<ICommunicationObject>(this.communicationObjects);
					this.communicationObjects.Clear();
				}
			}
			return new Microsoft.ServiceBus.Messaging.CloseCollectionAsyncResult(communicationObjects, timeout, callback, state);
		}

		public void EndClose(IAsyncResult result)
		{
			Microsoft.ServiceBus.Messaging.CloseCollectionAsyncResult.End(result);
		}

		private void Fault()
		{
			EventHandler eventHandler;
			lock (this.ThisLock)
			{
				if (this.faulted || this.closed)
				{
					eventHandler = null;
				}
				else
				{
					this.faulted = true;
					eventHandler = this.Faulted;
				}
			}
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		private void OnInnerObjectClosed(object sender, EventArgs args)
		{
			if (!this.closed)
			{
				lock (this.ThisLock)
				{
					if (!this.closed)
					{
						ICommunicationObject communicationObject = (ICommunicationObject)sender;
						this.communicationObjects.Remove(communicationObject);
						communicationObject.Closed -= this.onInnerObjectClosed;
						communicationObject.Faulted -= this.onInnerObjectFaulted;
					}
				}
			}
		}

		private void OnInnerObjectFaulted(object sender, EventArgs args)
		{
			this.Fault();
		}

		public bool Remove(ICommunicationObject communicationObject)
		{
			bool flag = this.communicationObjects.Remove(communicationObject);
			if (flag)
			{
				communicationObject.Faulted -= this.onInnerObjectFaulted;
				communicationObject.Closed -= this.onInnerObjectClosed;
			}
			return flag;
		}

		public event EventHandler Faulted;
	}
}