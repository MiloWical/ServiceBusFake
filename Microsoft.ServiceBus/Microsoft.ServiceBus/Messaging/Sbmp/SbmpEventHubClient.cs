using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class SbmpEventHubClient : EventHubClient
	{
		public SbmpEventHubClient(SbmpMessagingFactory messagingFactory, string path) : base(messagingFactory, path)
		{
		}

		protected override Task<MessageSender> CreateSenderAsync()
		{
			return base.MessagingFactory.CreateMessageSenderAsync(base.Path);
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
	}
}