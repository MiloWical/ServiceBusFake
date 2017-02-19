using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Properties;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal class InputChannel : Microsoft.ServiceBus.Channels.InputQueueChannel<Message>, IInputChannel, IChannel, ICommunicationObject
	{
		private EndpointAddress localAddress;

		public EndpointAddress LocalAddress
		{
			get
			{
				return this.localAddress;
			}
		}

		public InputChannel(ChannelManagerBase channelManager, EndpointAddress localAddress) : base(channelManager)
		{
			this.localAddress = localAddress;
		}

		public virtual IAsyncResult BeginReceive(AsyncCallback callback, object state)
		{
			return this.BeginReceive(base.DefaultReceiveTimeout, callback, state);
		}

		public virtual IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return Microsoft.ServiceBus.Channels.InputChannel.HelpBeginReceive(this, timeout, callback, state);
		}

		public virtual IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return base.BeginDequeue(timeout, callback, state);
		}

		public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return base.BeginWaitForItem(timeout, callback, state);
		}

		private static Exception CreateReceiveTimedOutException(IInputChannel channel, TimeSpan timeout)
		{
			if (channel.LocalAddress == null)
			{
				string receiveTimedOutNoLocalAddress = Resources.ReceiveTimedOutNoLocalAddress;
				object[] objArray = new object[] { timeout };
				return new TimeoutException(Microsoft.ServiceBus.SR.GetString(receiveTimedOutNoLocalAddress, objArray));
			}
			string receiveTimedOut = Resources.ReceiveTimedOut;
			object[] absoluteUri = new object[] { channel.LocalAddress.Uri.AbsoluteUri, timeout };
			return new TimeoutException(Microsoft.ServiceBus.SR.GetString(receiveTimedOut, absoluteUri));
		}

		public Message EndReceive(IAsyncResult result)
		{
			return Microsoft.ServiceBus.Channels.InputChannel.HelpEndReceive(result);
		}

		public virtual bool EndTryReceive(IAsyncResult result, out Message message)
		{
			return base.EndDequeue(result, out message);
		}

		public bool EndWaitForMessage(IAsyncResult result)
		{
			return base.EndWaitForItem(result);
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(IInputChannel))
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

		internal static IAsyncResult HelpBeginReceive(IInputChannel channel, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.InputChannel.HelpReceiveAsyncResult(channel, timeout, callback, state);
		}

		internal static Message HelpEndReceive(IAsyncResult result)
		{
			return Microsoft.ServiceBus.Channels.InputChannel.HelpReceiveAsyncResult.End(result);
		}

		internal static Message HelpReceive(IInputChannel channel, TimeSpan timeout)
		{
			Message message;
			if (!channel.TryReceive(timeout, out message))
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(Microsoft.ServiceBus.Channels.InputChannel.CreateReceiveTimedOutException(channel, timeout));
			}
			return message;
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
		}

		public virtual Message Receive()
		{
			return this.Receive(base.DefaultReceiveTimeout);
		}

		public virtual Message Receive(TimeSpan timeout)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return Microsoft.ServiceBus.Channels.InputChannel.HelpReceive(this, timeout);
		}

		public virtual bool TryReceive(TimeSpan timeout, out Message message)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return base.Dequeue(timeout, out message);
		}

		public bool WaitForMessage(TimeSpan timeout)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return base.WaitForItem(timeout);
		}

		private class HelpReceiveAsyncResult : AsyncResult
		{
			private IInputChannel channel;

			private TimeSpan timeout;

			private static AsyncCallback onReceive;

			private Message message;

			static HelpReceiveAsyncResult()
			{
				Microsoft.ServiceBus.Channels.InputChannel.HelpReceiveAsyncResult.onReceive = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.InputChannel.HelpReceiveAsyncResult.OnReceive));
			}

			public HelpReceiveAsyncResult(IInputChannel channel, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.channel = channel;
				this.timeout = timeout;
				IAsyncResult asyncResult = channel.BeginTryReceive(timeout, Microsoft.ServiceBus.Channels.InputChannel.HelpReceiveAsyncResult.onReceive, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return;
				}
				this.HandleReceiveComplete(asyncResult);
				base.Complete(true);
			}

			public static new Message End(IAsyncResult result)
			{
				return AsyncResult.End<Microsoft.ServiceBus.Channels.InputChannel.HelpReceiveAsyncResult>(result).message;
			}

			private void HandleReceiveComplete(IAsyncResult result)
			{
				if (!this.channel.EndTryReceive(result, out this.message))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(Microsoft.ServiceBus.Channels.InputChannel.CreateReceiveTimedOutException(this.channel, this.timeout));
				}
			}

			private static void OnReceive(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.InputChannel.HelpReceiveAsyncResult asyncState = (Microsoft.ServiceBus.Channels.InputChannel.HelpReceiveAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.HandleReceiveComplete(result);
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