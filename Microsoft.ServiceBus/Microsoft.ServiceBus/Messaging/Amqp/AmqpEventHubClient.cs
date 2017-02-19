using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.AmqpClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpEventHubClient : EventHubClient
	{
		private AmqpMessagingFactory amqpMessagingFactory;

		public AmqpEventHubClient(AmqpMessagingFactory messagingFactory, string name) : base(messagingFactory, name)
		{
			this.amqpMessagingFactory = messagingFactory;
		}

		private IAsyncResult BeginGetRuntimeInfo(TimeSpan timeout, AsyncCallback callback, object state)
		{
			AmqpEventHubClient.GetRuntimeInfoAsyncResult getRuntimeInfoAsyncResult = new AmqpEventHubClient.GetRuntimeInfoAsyncResult(this, timeout, callback, state);
			getRuntimeInfoAsyncResult.Start();
			return getRuntimeInfoAsyncResult;
		}

		protected override Task<MessageSender> CreateSenderAsync()
		{
			return base.MessagingFactory.CreateMessageSenderAsync(base.Path);
		}

		private EventHubRuntimeInformation EndGetRuntimeInfo(IAsyncResult asyncResult)
		{
			return AsyncResult<AmqpEventHubClient.GetRuntimeInfoAsyncResult>.End(asyncResult).RuntimeInfo;
		}

		public override EventHubRuntimeInformation GetRuntimeInformation()
		{
			AmqpEventHubClient.GetRuntimeInfoAsyncResult getRuntimeInfoAsyncResult = new AmqpEventHubClient.GetRuntimeInfoAsyncResult(this, this.OperationTimeout, null, null);
			return getRuntimeInfoAsyncResult.RunSynchronously().RuntimeInfo;
		}

		public override Task<EventHubRuntimeInformation> GetRuntimeInformationAsync()
		{
			return TaskHelpers.CreateTask<EventHubRuntimeInformation>((AsyncCallback c, object s) => this.BeginGetRuntimeInfo(this.OperationTimeout, c, s), (IAsyncResult r) => this.EndGetRuntimeInfo(r));
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

		private sealed class GetRuntimeInfoAsyncResult : IteratorAsyncResult<AmqpEventHubClient.GetRuntimeInfoAsyncResult>
		{
			private AmqpEventHubClient client;

			public EventHubRuntimeInformation RuntimeInfo
			{
				get;
				private set;
			}

			public GetRuntimeInfoAsyncResult(AmqpEventHubClient client, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.client = client;
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpEventHubClient.GetRuntimeInfoAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				this.client.ThrowIfDisposed();
				AmqpManagementLink amqpManagementLink = null;
				AmqpEventHubClient.GetRuntimeInfoAsyncResult getRuntimeInfoAsyncResult = this;
				yield return getRuntimeInfoAsyncResult.CallAsync((AmqpEventHubClient.GetRuntimeInfoAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.client.amqpMessagingFactory.BeginCreateManagementLink(t, c, s), (AmqpEventHubClient.GetRuntimeInfoAsyncResult thisPtr, IAsyncResult r) => amqpManagementLink = thisPtr.client.amqpMessagingFactory.EndCreateManagementLink(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				yield return base.CallAsync((AmqpEventHubClient.GetRuntimeInfoAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => amqpManagementLink.BeginGetEventHubRuntimeInfo(thisPtr.client.Path, t, c, s), (AmqpEventHubClient.GetRuntimeInfoAsyncResult thisPtr, IAsyncResult r) => thisPtr.RuntimeInfo = amqpManagementLink.EndGetEventHubRuntimeInfo(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}
	}
}