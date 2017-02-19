using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class OnMessageOptions
	{
		private int maxConcurrentCalls;

		private TimeSpan autoRenewTimeout;

		public bool AutoComplete
		{
			get;
			set;
		}

		internal bool AutoRenewLock
		{
			get
			{
				return this.AutoRenewTimeout > TimeSpan.Zero;
			}
		}

		public TimeSpan AutoRenewTimeout
		{
			get
			{
				return this.autoRenewTimeout;
			}
			set
			{
				Microsoft.ServiceBus.Common.TimeoutHelper.ThrowIfNegativeArgument(value, "value");
				this.autoRenewTimeout = value;
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

		public OnMessageOptions()
		{
			this.MaxConcurrentCalls = 1;
			this.AutoComplete = true;
			this.ReceiveTimeOut = Constants.DefaultOperationTimeout;
			this.AutoRenewTimeout = Constants.ClientPumpRenewLockTimeout;
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