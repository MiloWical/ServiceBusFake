using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Properties;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class OutputChannel : ChannelBase, IOutputChannel, IChannel, ICommunicationObjectInternals, ICommunicationObject
	{
		public abstract EndpointAddress RemoteAddress
		{
			get;
		}

		public abstract Uri Via
		{
			get;
		}

		protected OutputChannel(ChannelManagerBase manager) : base(manager)
		{
		}

		protected virtual void AddHeadersTo(Message message)
		{
		}

		public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
		{
			return this.BeginSend(message, base.DefaultSendTimeout, callback, state);
		}

		public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (message == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("message");
			}
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			base.ThrowIfDisposedOrNotOpen();
			this.AddHeadersTo(message);
			Microsoft.ServiceBus.Channels.OutputChannel.EmitTrace(message);
			return this.OnBeginSend(message, timeout, callback, state);
		}

		private static void EmitTrace(Message message)
		{
		}

		public void EndSend(IAsyncResult result)
		{
			this.OnEndSend(result);
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(IOutputChannel))
			{
				return (T)this;
			}
			T property = base.GetProperty<T>();
			if (property != null)
			{
				return property;
			}
			return default(T);
		}

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposed()
		{
			base.ThrowIfDisposed();
		}

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposedOrNotOpen()
		{
			base.ThrowIfDisposedOrNotOpen();
		}

		protected abstract IAsyncResult OnBeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract void OnEndSend(IAsyncResult result);

		protected abstract void OnSend(Message message, TimeSpan timeout);

		public void Send(Message message)
		{
			this.Send(message, base.DefaultSendTimeout);
		}

		public void Send(Message message, TimeSpan timeout)
		{
			if (message == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("message");
			}
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			base.ThrowIfDisposedOrNotOpen();
			this.AddHeadersTo(message);
			Microsoft.ServiceBus.Channels.OutputChannel.EmitTrace(message);
			this.OnSend(message, timeout);
		}
	}
}