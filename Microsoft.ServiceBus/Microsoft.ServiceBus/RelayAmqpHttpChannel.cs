using Microsoft.ServiceBus.Messaging.Channels;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class RelayAmqpHttpChannel : ChannelBase, IReplyChannel, IChannel, ICommunicationObjectInternals, ICommunicationObject
	{
		public EndpointAddress LocalAddress
		{
			get
			{
				return JustDecompileGenerated_get_LocalAddress();
			}
			set
			{
				JustDecompileGenerated_set_LocalAddress(value);
			}
		}

		private EndpointAddress JustDecompileGenerated_LocalAddress_k__BackingField;

		public EndpointAddress JustDecompileGenerated_get_LocalAddress()
		{
			return this.JustDecompileGenerated_LocalAddress_k__BackingField;
		}

		private void JustDecompileGenerated_set_LocalAddress(EndpointAddress value)
		{
			this.JustDecompileGenerated_LocalAddress_k__BackingField = value;
		}

		public RelayAmqpHttpChannel(ChannelManagerBase channelManager) : base(channelManager)
		{
			this.LocalAddress = new EndpointAddress("junk for now");
		}

		public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public IAsyncResult BeginTryReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public IAsyncResult BeginWaitForRequest(TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public RequestContext EndReceiveRequest(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		public bool EndTryReceiveRequest(IAsyncResult result, out RequestContext context)
		{
			context = null;
			throw new NotImplementedException();
		}

		public bool EndWaitForRequest(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposed()
		{
			base.ThrowIfDisposed();
		}

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposedOrNotOpen()
		{
			base.ThrowIfDisposedOrNotOpen();
		}

		protected override void OnAbort()
		{
			throw new NotImplementedException();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override void OnClose(TimeSpan timeout)
		{
			throw new NotImplementedException();
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			throw new NotImplementedException();
		}

		public RequestContext ReceiveRequest()
		{
			throw new NotImplementedException();
		}

		public RequestContext ReceiveRequest(TimeSpan timeout)
		{
			throw new NotImplementedException();
		}

		public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
		{
			context = null;
			throw new NotImplementedException();
		}

		public bool WaitForRequest(TimeSpan timeout)
		{
			throw new NotImplementedException();
		}
	}
}