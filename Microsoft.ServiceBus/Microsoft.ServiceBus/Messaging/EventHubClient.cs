using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Messaging.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class EventHubClient : ClientEntity
	{
		private readonly Lazy<MessagingFactorySettings> cachedSettings;

		private int prefetchCount;

		internal MessageSender InternalSender
		{
			get;
			set;
		}

		internal Microsoft.ServiceBus.Messaging.MessagingFactory MessagingFactory
		{
			get;
			private set;
		}

		internal override TimeSpan OperationTimeout
		{
			get
			{
				return this.MessagingFactory.OperationTimeout;
			}
		}

		public string Path
		{
			get;
			private set;
		}

		public int PrefetchCount
		{
			get
			{
				return this.prefetchCount;
			}
			set
			{
				EventHubClient.ValidateEventHubPrefetchCount(value);
				this.prefetchCount = value;
			}
		}

		internal EventHubClient(Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory, string path)
		{
			this.MessagingFactory = messagingFactory;
			this.cachedSettings = new Lazy<MessagingFactorySettings>(new Func<MessagingFactorySettings>(this.MessagingFactory.GetSettings));
			base.ClientEntityManager = new MessageClientEntityManager();
			this.Path = path;
			base.RetryPolicy = messagingFactory.RetryPolicy.Clone();
			this.PrefetchCount = Constants.DefaultEventHubPrefetchCount;
		}

		public static EventHubClient Create(string path)
		{
			KeyValueConfigurationManager keyValueConfigurationManager = new KeyValueConfigurationManager(new TransportType?(TransportType.Amqp));
			return keyValueConfigurationManager.CreateMessagingFactory().CreateEventHubClient(path);
		}

		public static EventHubClient CreateFromConnectionString(string connectionString, string path)
		{
			KeyValueConfigurationManager keyValueConfigurationManager = new KeyValueConfigurationManager(connectionString, new TransportType?(TransportType.Amqp));
			return keyValueConfigurationManager.CreateMessagingFactory().CreateEventHubClient(path);
		}

		public EventHubSender CreatePartitionedSender(string partitionId)
		{
			this.ThrowIfSbmpClient("CreatePartitionedSenderAsync");
			return new EventHubSender(this.MessagingFactory, this.Path, partitionId);
		}

		public Task<EventHubSender> CreatePartitionedSenderAsync(string partitionId)
		{
			this.ThrowIfSbmpClient("CreatePartitionedSenderAsync");
			return TaskHelpers.GetCompletedTask<EventHubSender>(new EventHubSender(this.MessagingFactory, this.Path, partitionId));
		}

		protected abstract Task<MessageSender> CreateSenderAsync();

		private void EnsureCreateInternalSender()
		{
			base.ThrowIfDisposed();
			if (this.InternalSender == null)
			{
				lock (base.ThisLock)
				{
					if (this.InternalSender == null)
					{
						this.InternalSender = this.CreateSenderAsync().Result;
						this.InternalSender.RetryPolicy = base.RetryPolicy.Clone();
						this.InternalSender.EntityType = new MessagingEntityType?(MessagingEntityType.EventHub);
						this.RegisterMessageClientEntity(this.InternalSender);
					}
				}
			}
		}

		public EventHubConsumerGroup GetConsumerGroup(string groupName)
		{
			base.ThrowIfDisposed();
			if (string.IsNullOrWhiteSpace(groupName))
			{
				throw Fx.Exception.ArgumentNullOrWhiteSpace("groupName");
			}
			EventHubConsumerGroup eventHubConsumerGroup = new EventHubConsumerGroup(this.MessagingFactory, this.Path, groupName, this.PrefetchCount);
			this.RegisterMessageClientEntity(eventHubConsumerGroup);
			return eventHubConsumerGroup;
		}

		public EventHubConsumerGroup GetDefaultConsumerGroup()
		{
			return this.GetConsumerGroup("$Default");
		}

		public virtual EventHubRuntimeInformation GetRuntimeInformation()
		{
			throw new NotSupportedException(SRCore.UnsupportedTransport("GetRuntimeInformation", TransportType.NetMessaging.ToString()));
		}

		public virtual Task<EventHubRuntimeInformation> GetRuntimeInformationAsync()
		{
			throw new NotSupportedException(SRCore.UnsupportedTransport("GetRuntimeInformationAsync", TransportType.NetMessaging.ToString()));
		}

		protected override void OnAbort()
		{
			EventHubClient.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new EventHubClient.CloseOrAbortAsyncResult(this, true, TimeSpan.MaxValue, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new EventHubClient.CloseOrAbortAsyncResult(this, false, timeout, callback, state)).Start();
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			EventHubClient.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new EventHubClient.CloseOrAbortAsyncResult(this, false, timeout, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<EventHubClient.CloseOrAbortAsyncResult>.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		internal void RegisterMessageClientEntity(ClientEntity child)
		{
			base.ClientEntityManager.Add(child);
		}

		public void Send(EventData data)
		{
			this.ThrowIfSenderNull("Send");
			MessageSender internalSender = this.InternalSender;
			MessageSender messageSender = this.InternalSender;
			EventData[] eventDataArray = new EventData[] { data };
			internalSender.EndSendEventData(messageSender.BeginSendEventData(null, eventDataArray, this.OperationTimeout, null, null));
		}

		public Task SendAsync(EventData data)
		{
			this.ThrowIfSenderNull("Send");
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.InternalSender.BeginSendEventData(null, new EventData[] { data }, this.OperationTimeout, c, s), new Action<IAsyncResult>(this.InternalSender.EndSendEventData));
		}

		public void SendBatch(IEnumerable<EventData> eventDataList)
		{
			this.ThrowIfSenderNull("SendBatch");
			this.InternalSender.EndSendEventData(this.InternalSender.BeginSendEventData(null, eventDataList, this.OperationTimeout, null, null));
		}

		public Task SendBatchAsync(IEnumerable<EventData> eventDataList)
		{
			this.ThrowIfSenderNull("SendBatchAsync");
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.InternalSender.BeginSendEventData(null, eventDataList, this.OperationTimeout, c, s), new Action<IAsyncResult>(this.InternalSender.EndSendEventData));
		}

		private void ThrowIfSbmpClient(string operationName)
		{
			if (this.cachedSettings.Value.TransportType == TransportType.NetMessaging)
			{
				throw new NotSupportedException(SRCore.UnsupportedTransport(operationName, TransportType.NetMessaging.ToString()));
			}
		}

		private void ThrowIfSenderNull(string operationName)
		{
			this.EnsureCreateInternalSender();
			if (this.InternalSender == null)
			{
				throw FxTrace.Exception.AsError(new InvalidOperationException(SRCore.UnsupportedOperation(operationName)), null);
			}
		}

		internal static void ValidateEventHubPrefetchCount(int value)
		{
			if (value < Constants.EventHubMinimumPrefetchCount)
			{
				throw FxTrace.Exception.ArgumentOutOfRange("PrefetchCount", value, SRClient.ArgumentOutOfRange(Constants.EventHubMinimumPrefetchCount, 2147483647));
			}
		}

		private sealed class CloseOrAbortAsyncResult : IteratorAsyncResult<EventHubClient.CloseOrAbortAsyncResult>
		{
			private readonly EventHubClient owner;

			private readonly bool shouldAbort;

			public CloseOrAbortAsyncResult(EventHubClient owner, bool shouldAbort, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.shouldAbort = shouldAbort;
			}

			protected override IEnumerator<IteratorAsyncResult<EventHubClient.CloseOrAbortAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (!this.shouldAbort)
				{
					EventHubClient.CloseOrAbortAsyncResult closeOrAbortAsyncResult = this;
					IteratorAsyncResult<EventHubClient.CloseOrAbortAsyncResult>.BeginCall beginCall = (EventHubClient.CloseOrAbortAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.ClientEntityManager.BeginClose(t, c, s);
					yield return closeOrAbortAsyncResult.CallAsync(beginCall, (EventHubClient.CloseOrAbortAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.ClientEntityManager.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				else
				{
					this.owner.ClientEntityManager.Abort();
				}
			}
		}
	}
}