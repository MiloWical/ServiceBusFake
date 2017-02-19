using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class SbmpVolatileTopicClient : VolatileTopicClient
	{
		private Lazy<SbmpMessageCreator> ControlMessageCreator
		{
			get;
			set;
		}

		public SbmpVolatileTopicClient(SbmpMessagingFactory factory, string path, string clientId, Microsoft.ServiceBus.RetryPolicy retryPolicy, Microsoft.ServiceBus.Messaging.Filter filter) : base(factory, path, clientId, retryPolicy, filter)
		{
			this.ControlMessageCreator = new Lazy<SbmpMessageCreator>(new Func<SbmpMessageCreator>(this.InitializeControlLink));
		}

		private SbmpMessageCreator InitializeControlLink()
		{
			CreateControlLinkSettings createControlLinkSetting = new CreateControlLinkSettings((SbmpMessagingFactory)base.MessagingFactory, base.Path, base.Path, MessagingEntityType.VolatileTopic, null);
			return createControlLinkSetting.MessageCreator;
		}

		protected override IAsyncResult OnBeginCreateReceiver(TimeSpan timeout, AsyncCallback callback, object state)
		{
			string str = EntityNameHelper.FormatSubscriptionPath(base.Path, base.ClientId);
			CreateReceiverLinkSettings createReceiverLinkSetting = new CreateReceiverLinkSettings((SbmpMessagingFactory)base.MessagingFactory, str, base.Path, new MessagingEntityType?(MessagingEntityType.VolatileTopicSubscription), ReceiveMode.ReceiveAndDelete, this.ControlMessageCreator, base.RetryPolicy, false);
			return new CompletedAsyncResult<SbmpMessageReceiver>(createReceiverLinkSetting.MessageReceiver, callback, state);
		}

		protected override IAsyncResult OnBeginCreateSender(TimeSpan timeout, AsyncCallback callback, object state)
		{
			CreateSenderLinkSettings createSenderLinkSetting = new CreateSenderLinkSettings((SbmpMessagingFactory)base.MessagingFactory, base.Path, new MessagingEntityType?(MessagingEntityType.VolatileTopic), base.RetryPolicy);
			return new CompletedAsyncResult<SbmpMessageSender>(createSenderLinkSetting.MessageSender, callback, state);
		}

		protected override MessageReceiver OnEndCreateReceiver(IAsyncResult result)
		{
			return CompletedAsyncResult<SbmpMessageReceiver>.End(result);
		}

		protected override MessageSender OnEndCreateSender(IAsyncResult result)
		{
			return CompletedAsyncResult<SbmpMessageSender>.End(result);
		}
	}
}