using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class SocketConnectionReplyChannelListener : SocketConnectionChannelListener<IReplyChannel, Microsoft.ServiceBus.Channels.ReplyChannelAcceptor>, Microsoft.ServiceBus.Channels.ISingletonChannelListener
	{
		private Microsoft.ServiceBus.Channels.ReplyChannelAcceptor replyAcceptor;

		protected override Microsoft.ServiceBus.Channels.ReplyChannelAcceptor ChannelAcceptor
		{
			get
			{
				return this.replyAcceptor;
			}
		}

		TimeSpan Microsoft.ServiceBus.Channels.ISingletonChannelListener.ReceiveTimeout
		{
			get
			{
				return ((IDefaultCommunicationTimeouts)this).ReceiveTimeout;
			}
		}

		public SocketConnectionReplyChannelListener(SocketConnectionBindingElement bindingElement, BindingContext context) : base(bindingElement, context)
		{
			this.replyAcceptor = new Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener.ConnectionOrientedTransportReplyChannelAcceptor(this);
		}

		void Microsoft.ServiceBus.Channels.ISingletonChannelListener.ReceiveRequest(RequestContext requestContext, Action callback, bool canDispatchOnThisThread)
		{
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceVerbose)
			{
				TraceUtility.TraceEvent(TraceEventType.Verbose, this.MessageReceivedTraceCode, requestContext.RequestMessage);
			}
			this.replyAcceptor.Enqueue(requestContext, callback, canDispatchOnThisThread);
		}
	}
}