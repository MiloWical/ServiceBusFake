using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal sealed class ServiceBusInputSessionChannel : ServiceBusInputChannelBase<IInputSessionChannel>, IInputSessionChannel, IInputChannel, IChannel, ICommunicationObject, ISessionChannel<IInputSession>
	{
		private TimeSpan sessionIdleTimeout;

		public IInputSession Session
		{
			get
			{
				return JustDecompileGenerated_get_Session();
			}
			set
			{
				JustDecompileGenerated_set_Session(value);
			}
		}

		private IInputSession JustDecompileGenerated_Session_k__BackingField;

		public IInputSession JustDecompileGenerated_get_Session()
		{
			return this.JustDecompileGenerated_Session_k__BackingField;
		}

		private void JustDecompileGenerated_set_Session(IInputSession value)
		{
			this.JustDecompileGenerated_Session_k__BackingField = value;
		}

		public ServiceBusInputSessionChannel(MessageSession messageSession, ServiceBusChannelListener<IInputSessionChannel> parent) : base(parent)
		{
			this.Session = new ServiceBusInputSessionChannel.InputSession(messageSession.SessionId);
			base.MessageReceiver = messageSession;
			this.sessionIdleTimeout = parent.TransportBindingElement.SessionIdleTimeout;
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.MessageReceiver.BeginOpen(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new ServiceBusInputSessionChannel.TryReceiveAsyncResult(this, timeout, callback, state)).Start();
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			base.MessageReceiver.EndOpen(result);
		}

		protected override bool OnEndTryReceive(IAsyncResult result, out BrokeredMessage brokeredMessage)
		{
			ServiceBusInputSessionChannel.TryReceiveAsyncResult tryReceiveAsyncResult = AsyncResult<ServiceBusInputSessionChannel.TryReceiveAsyncResult>.End(result);
			brokeredMessage = tryReceiveAsyncResult.BrokeredMessage;
			return tryReceiveAsyncResult.ReturnValue;
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			base.MessageReceiver.Open(timeout);
		}

		protected override bool OnTryReceive(TimeSpan timeout, out BrokeredMessage brokeredMessage)
		{
			ServiceBusInputSessionChannel.TryReceiveAsyncResult tryReceiveAsyncResult = new ServiceBusInputSessionChannel.TryReceiveAsyncResult(this, timeout, null, null);
			tryReceiveAsyncResult.RunSynchronously();
			brokeredMessage = tryReceiveAsyncResult.BrokeredMessage;
			return tryReceiveAsyncResult.ReturnValue;
		}

		private sealed class InputSession : IInputSession, ISession
		{
			public string Id
			{
				get
				{
					return get_Id();
				}
				set
				{
					set_Id(value);
				}
			}

			private string <Id>k__BackingField;

			public string get_Id()
			{
				return this.<Id>k__BackingField;
			}

			private void set_Id(string value)
			{
				this.<Id>k__BackingField = value;
			}

			public InputSession(string id)
			{
				this.Id = id;
			}
		}

		private class TryReceiveAsyncResult : IteratorAsyncResult<ServiceBusInputSessionChannel.TryReceiveAsyncResult>
		{
			private readonly ServiceBusInputSessionChannel sessionChannel;

			private readonly TimeSpan innerTimeout;

			private bool returnValue;

			private BrokeredMessage brokeredMessage;

			public BrokeredMessage BrokeredMessage
			{
				get
				{
					return this.brokeredMessage;
				}
			}

			public bool ReturnValue
			{
				get
				{
					return this.returnValue;
				}
			}

			public TryReceiveAsyncResult(ServiceBusInputSessionChannel sessionChannel, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.sessionChannel = sessionChannel;
				this.innerTimeout = TimeoutHelper.Min(timeout, this.sessionChannel.sessionIdleTimeout);
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusInputSessionChannel.TryReceiveAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				ServiceBusInputSessionChannel.TryReceiveAsyncResult tryReceiveAsyncResult = this;
				IteratorAsyncResult<ServiceBusInputSessionChannel.TryReceiveAsyncResult>.BeginCall beginCall = (ServiceBusInputSessionChannel.TryReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.sessionChannel.MessageReceiver.BeginTryReceive(thisPtr.innerTimeout, c, s);
				IteratorAsyncResult<ServiceBusInputSessionChannel.TryReceiveAsyncResult>.EndCall endCall = (ServiceBusInputSessionChannel.TryReceiveAsyncResult thisPtr, IAsyncResult a) => thisPtr.returnValue = thisPtr.sessionChannel.MessageReceiver.EndTryReceive(a, out thisPtr.brokeredMessage);
				yield return tryReceiveAsyncResult.CallAsync(beginCall, endCall, (ServiceBusInputSessionChannel.TryReceiveAsyncResult thisPtr, TimeSpan t) => thisPtr.returnValue = thisPtr.sessionChannel.MessageReceiver.TryReceive(thisPtr.innerTimeout, out thisPtr.brokeredMessage), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				if (!this.returnValue && base.OriginalTimeout > this.sessionChannel.sessionIdleTimeout)
				{
					this.returnValue = true;
				}
			}
		}
	}
}