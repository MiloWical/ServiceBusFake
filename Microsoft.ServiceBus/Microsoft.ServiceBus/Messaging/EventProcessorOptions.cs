using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	public class EventProcessorOptions
	{
		private readonly static int DefaultMaxBatchSize;

		private readonly static TimeSpan DefaultReceiveTimeout;

		private readonly static Func<string, object> DefaultInitialOffsetProvider;

		public static EventProcessorOptions DefaultOptions
		{
			get
			{
				return new EventProcessorOptions();
			}
		}

		public Func<string, object> InitialOffsetProvider
		{
			get;
			set;
		}

		public int MaxBatchSize
		{
			get;
			set;
		}

		public int PrefetchCount
		{
			get;
			set;
		}

		public TimeSpan ReceiveTimeOut
		{
			get;
			set;
		}

		static EventProcessorOptions()
		{
			EventProcessorOptions.DefaultMaxBatchSize = 10;
			EventProcessorOptions.DefaultReceiveTimeout = TimeSpan.FromMinutes(1);
			EventProcessorOptions.DefaultInitialOffsetProvider = (string partitionId) => "-1";
		}

		public EventProcessorOptions()
		{
			this.MaxBatchSize = EventProcessorOptions.DefaultMaxBatchSize;
			this.ReceiveTimeOut = EventProcessorOptions.DefaultReceiveTimeout;
			this.InitialOffsetProvider = EventProcessorOptions.DefaultInitialOffsetProvider;
			this.PrefetchCount = Constants.DefaultEventHubPrefetchCount;
		}

		internal void RaiseExceptionReceived(IEventProcessor processor, ExceptionReceivedEventArgs e)
		{
			EventHandler<ExceptionReceivedEventArgs> eventHandler = this.ExceptionReceived;
			if (eventHandler != null)
			{
				eventHandler(processor, e);
			}
		}

		public event EventHandler<ExceptionReceivedEventArgs> ExceptionReceived;
	}
}