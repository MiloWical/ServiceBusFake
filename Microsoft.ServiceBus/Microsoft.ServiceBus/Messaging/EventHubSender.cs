using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class EventHubSender : ClientEntity
	{
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

		public string PartitionId
		{
			get;
			private set;
		}

		public string Path
		{
			get;
			private set;
		}

		internal EventHubSender(Microsoft.ServiceBus.Messaging.MessagingFactory factory, string path, string partitionId)
		{
			if (factory == null)
			{
				throw Fx.Exception.ArgumentNull("factory");
			}
			if (string.IsNullOrWhiteSpace(path))
			{
				throw Fx.Exception.ArgumentNullOrWhiteSpace("path");
			}
			this.Path = path;
			this.PartitionId = partitionId;
			this.MessagingFactory = factory;
			base.RetryPolicy = factory.RetryPolicy.Clone();
		}

		private void EnsureCreateInternalSender()
		{
			base.ThrowIfDisposed();
			if (this.InternalSender == null)
			{
				lock (base.ThisLock)
				{
					if (this.InternalSender == null)
					{
						try
						{
							this.InternalSender = this.MessagingFactory.CreateSenderAsync(this.Path, this.PartitionId).Result;
							this.InternalSender.RetryPolicy = base.RetryPolicy;
							this.InternalSender.EntityType = new MessagingEntityType?(MessagingEntityType.EventHub);
						}
						catch (AggregateException aggregateException)
						{
							throw aggregateException.Flatten().InnerException;
						}
					}
				}
			}
		}

		protected override void OnAbort()
		{
			if (this.InternalSender != null)
			{
				this.InternalSender.Abort();
			}
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.InternalSender == null)
			{
				return new CompletedAsyncResult(callback, state);
			}
			return this.InternalSender.BeginClose(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfSenderNull("BeginOpen");
			return this.InternalSender.BeginOpen(timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			if (this.InternalSender != null)
			{
				this.InternalSender.Close(timeout);
			}
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			if (this.InternalSender == null)
			{
				CompletedAsyncResult.End(result);
				return;
			}
			this.InternalSender.EndClose(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			this.InternalSender.EndOpen(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			this.ThrowIfSenderNull("Open");
			this.InternalSender.Open(timeout);
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

		private void ThrowIfSenderNull(string operationName)
		{
			this.EnsureCreateInternalSender();
			if (this.InternalSender == null)
			{
				throw FxTrace.Exception.AsError(new InvalidOperationException(SRCore.UnsupportedOperation(operationName)), null);
			}
		}
	}
}