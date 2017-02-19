using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class LayeredChannelListener<TChannel> : ChannelListenerBaseInternals<TChannel>
	where TChannel : class, IChannel
	{
		private IChannelListener innerChannelListener;

		private bool sharedInnerListener;

		private EventHandler onInnerListenerFaulted;

		internal virtual IChannelListener InnerChannelListener
		{
			get
			{
				return this.innerChannelListener;
			}
			set
			{
				lock (base.ThisLock)
				{
					base.ThrowIfDisposedOrImmutable();
					if (this.innerChannelListener != null)
					{
						this.innerChannelListener.Faulted -= this.onInnerListenerFaulted;
					}
					this.innerChannelListener = value;
					if (this.innerChannelListener != null)
					{
						this.innerChannelListener.Faulted += this.onInnerListenerFaulted;
					}
				}
			}
		}

		internal bool SharedInnerListener
		{
			get
			{
				return this.sharedInnerListener;
			}
		}

		public override System.Uri Uri
		{
			get
			{
				return this.GetInnerListenerSnapshot().Uri;
			}
		}

		protected LayeredChannelListener(IDefaultCommunicationTimeouts timeouts, IChannelListener innerChannelListener) : this(false, timeouts, innerChannelListener)
		{
		}

		protected LayeredChannelListener(bool sharedInnerListener, IDefaultCommunicationTimeouts timeouts) : this(sharedInnerListener, timeouts, null)
		{
		}

		protected LayeredChannelListener(bool sharedInnerListener, IDefaultCommunicationTimeouts timeouts, IChannelListener innerChannelListener) : base(timeouts)
		{
			this.sharedInnerListener = sharedInnerListener;
			this.innerChannelListener = innerChannelListener;
			this.onInnerListenerFaulted = new EventHandler(this.OnInnerListenerFaulted);
			if (this.innerChannelListener != null)
			{
				this.innerChannelListener.Faulted += this.onInnerListenerFaulted;
			}
		}

		internal IChannelListener GetInnerListenerSnapshot()
		{
			IChannelListener innerChannelListener = this.InnerChannelListener;
			if (innerChannelListener == null)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string innerListenerFactoryNotSet = Resources.InnerListenerFactoryNotSet;
				object[] str = new object[] { base.GetType().ToString() };
				throw exceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(innerListenerFactoryNotSet, str)));
			}
			return innerChannelListener;
		}

		public override T GetProperty<T>()
		where T : class
		{
			T property = base.GetProperty<T>();
			if (property != null)
			{
				return property;
			}
			IChannelListener innerChannelListener = this.InnerChannelListener;
			if (innerChannelListener != null)
			{
				return innerChannelListener.GetProperty<T>();
			}
			return default(T);
		}

		protected override void OnAbort()
		{
			lock (base.ThisLock)
			{
				this.OnCloseOrAbort();
			}
			IChannelListener innerChannelListener = this.InnerChannelListener;
			if (innerChannelListener != null && !this.sharedInnerListener)
			{
				innerChannelListener.Abort();
			}
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.OnCloseOrAbort();
			return new Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.CloseAsyncResult(this.InnerChannelListener, this.sharedInnerListener, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.OpenAsyncResult(this.InnerChannelListener, this.sharedInnerListener, timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.OnCloseOrAbort();
			if (this.InnerChannelListener != null && !this.sharedInnerListener)
			{
				this.InnerChannelListener.Close(timeout);
			}
		}

		private void OnCloseOrAbort()
		{
			IChannelListener innerChannelListener = this.InnerChannelListener;
			if (innerChannelListener != null)
			{
				innerChannelListener.Faulted -= this.onInnerListenerFaulted;
			}
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.CloseAsyncResult>.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			AsyncResult<Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.OpenAsyncResult>.End(result);
		}

		private void OnInnerListenerFaulted(object sender, EventArgs e)
		{
			base.Fault();
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			if (this.InnerChannelListener != null && !this.sharedInnerListener)
			{
				this.InnerChannelListener.Open(timeout);
			}
		}

		protected override void OnOpening()
		{
			base.OnOpening();
			this.ThrowIfInnerListenerNotSet();
		}

		internal void ThrowIfInnerListenerNotSet()
		{
			if (this.InnerChannelListener == null)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string innerListenerFactoryNotSet = Resources.InnerListenerFactoryNotSet;
				object[] str = new object[] { base.GetType().ToString() };
				throw exceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(innerListenerFactoryNotSet, str)));
			}
		}

		private class CloseAsyncResult : AsyncResult<Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.CloseAsyncResult>
		{
			private ICommunicationObject communicationObject;

			private static AsyncCallback onCloseComplete;

			static CloseAsyncResult()
			{
				Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.CloseAsyncResult.onCloseComplete = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.CloseAsyncResult.OnCloseComplete));
			}

			public CloseAsyncResult(ICommunicationObject communicationObject, bool sharedInnerListener, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.communicationObject = communicationObject;
				if (this.communicationObject == null || sharedInnerListener)
				{
					base.Complete(true);
					return;
				}
				IAsyncResult asyncResult = this.communicationObject.BeginClose(timeout, Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.CloseAsyncResult.onCloseComplete, this);
				if (asyncResult.CompletedSynchronously)
				{
					this.communicationObject.EndClose(asyncResult);
					base.Complete(true);
				}
			}

			private static void OnCloseComplete(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.CloseAsyncResult asyncState = (Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.CloseAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.communicationObject.EndClose(result);
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
				asyncState.Complete(false, exception);
			}
		}

		private class OpenAsyncResult : AsyncResult<Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.OpenAsyncResult>
		{
			private ICommunicationObject communicationObject;

			private static AsyncCallback onOpenComplete;

			static OpenAsyncResult()
			{
				Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.OpenAsyncResult.onOpenComplete = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.OpenAsyncResult.OnOpenComplete));
			}

			public OpenAsyncResult(ICommunicationObject communicationObject, bool sharedInnerListener, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.communicationObject = communicationObject;
				if (this.communicationObject == null || sharedInnerListener)
				{
					base.Complete(true);
					return;
				}
				IAsyncResult asyncResult = this.communicationObject.BeginOpen(timeout, Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.OpenAsyncResult.onOpenComplete, this);
				if (asyncResult.CompletedSynchronously)
				{
					this.communicationObject.EndOpen(asyncResult);
					base.Complete(true);
				}
			}

			private static void OnOpenComplete(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.OpenAsyncResult asyncState = (Microsoft.ServiceBus.Channels.LayeredChannelListener<TChannel>.OpenAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.communicationObject.EndOpen(result);
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
				asyncState.Complete(false, exception);
			}
		}
	}
}