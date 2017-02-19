using Microsoft.ServiceBus.Channels;
using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class RelayHttpTransportChannelListener : Microsoft.ServiceBus.Channels.LayeredChannelListener<IReplyChannel>
	{
		private readonly IChannelListener<IReplyChannel> innerChannelListener;

		private readonly System.Uri listenUri;

		private readonly MessageEncoder encoder;

		private readonly bool preserveRawHttp;

		public override System.Uri Uri
		{
			get
			{
				return this.listenUri;
			}
		}

		public RelayHttpTransportChannelListener(BindingContext context, MessageEncodingBindingElement encodingBindingElement, IChannelListener<IReplyChannel> innerChannelListener) : base(context.Binding, innerChannelListener)
		{
			this.innerChannelListener = innerChannelListener;
			this.listenUri = this.innerChannelListener.Uri;
			this.encoder = encodingBindingElement.CreateMessageEncoderFactory().Encoder;
			this.preserveRawHttp = context.BindingParameters.Find<NameSettings>().ServiceSettings.PreserveRawHttp;
		}

		public override T GetProperty<T>()
		where T : class
		{
			return this.innerChannelListener.GetProperty<T>();
		}

		protected override IReplyChannel OnAcceptChannel(TimeSpan timeout)
		{
			return this.WrapInnerChannel(this.innerChannelListener.AcceptChannel(timeout));
		}

		protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.innerChannelListener.BeginAcceptChannel(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.innerChannelListener.BeginWaitForChannel(timeout, callback, state);
		}

		protected override IReplyChannel OnEndAcceptChannel(IAsyncResult result)
		{
			return this.WrapInnerChannel(this.innerChannelListener.EndAcceptChannel(result));
		}

		protected override bool OnEndWaitForChannel(IAsyncResult result)
		{
			return this.innerChannelListener.EndWaitForChannel(result);
		}

		protected override bool OnWaitForChannel(TimeSpan timeout)
		{
			return this.innerChannelListener.WaitForChannel(timeout);
		}

		private IReplyChannel WrapInnerChannel(IReplyChannel innerChannel)
		{
			if (innerChannel == null)
			{
				return null;
			}
			return new RelayHttpTransportReplyChannel(this, innerChannel, this.encoder, this.preserveRawHttp);
		}
	}
}