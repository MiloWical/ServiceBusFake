using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class OnShardMessageOptions
	{
		private int maxConcurrentCalls;

		private TimeSpan autoCheckpointTimeout;

		internal bool AutoCheckpoint
		{
			get
			{
				return this.AutoCheckpointTimeout > TimeSpan.Zero;
			}
		}

		public TimeSpan AutoCheckpointTimeout
		{
			get
			{
				return this.autoCheckpointTimeout;
			}
			set
			{
				Microsoft.ServiceBus.Common.TimeoutHelper.ThrowIfNegativeArgument(value, "value");
				this.autoCheckpointTimeout = value;
			}
		}

		public int MaxConcurrentCalls
		{
			get
			{
				return this.maxConcurrentCalls;
			}
			set
			{
				if (value <= 0)
				{
					throw new ArgumentOutOfRangeException(SRClient.MaxConcurrentCallsMustBeGreaterThanZero(value));
				}
				this.maxConcurrentCalls = value;
			}
		}

		internal Microsoft.ServiceBus.Messaging.MessageClientEntity MessageClientEntity
		{
			get;
			set;
		}

		internal TimeSpan ReceiveTimeOut
		{
			get;
			set;
		}

		public OnShardMessageOptions()
		{
			this.MaxConcurrentCalls = 1;
			this.ReceiveTimeOut = Constants.DefaultOperationTimeout;
			this.AutoCheckpointTimeout = Constants.ClientPumpRenewLockTimeout;
		}

		internal void RaiseExceptionReceived(ExceptionReceivedEventArgs e)
		{
			EventHandler<ExceptionReceivedEventArgs> eventHandler = this.ExceptionReceived;
			if (eventHandler != null)
			{
				eventHandler(this.MessageClientEntity, e);
			}
		}

		public event EventHandler<ExceptionReceivedEventArgs> ExceptionReceived;
	}
}