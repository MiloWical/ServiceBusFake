using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	public class SessionHandlerOptions
	{
		private int maxConcurrentSessions;

		private int maxPendingAcceptSessionCalls;

		private TimeSpan messageWaitTimeout;

		private TimeSpan autoRenewTimeout;

		public bool AutoComplete
		{
			get;
			set;
		}

		public TimeSpan AutoRenewTimeout
		{
			get
			{
				return this.autoRenewTimeout;
			}
			set
			{
				Microsoft.ServiceBus.Common.TimeoutHelper.ThrowIfNonPositiveArgument(value, "value");
				this.autoRenewTimeout = value;
			}
		}

		public int MaxConcurrentSessions
		{
			get
			{
				return this.maxConcurrentSessions;
			}
			set
			{
				if (value <= 0)
				{
					throw new ArgumentOutOfRangeException("value", (object)value, SRClient.ValueMustBePositive(value));
				}
				this.maxConcurrentSessions = value;
				this.MaxPendingAcceptSessionCalls = Math.Min(value, Environment.ProcessorCount);
			}
		}

		internal int MaxPendingAcceptSessionCalls
		{
			get
			{
				return this.maxPendingAcceptSessionCalls;
			}
			set
			{
				if (this.MaxConcurrentSessions < value)
				{
					throw new ArgumentOutOfRangeException("value", (object)value, SRClient.PropertyMustBeEqualOrLessThanOtherProperty("MaxPendingAcceptSessionCalls", "MaxConcurrentSessions"));
				}
				this.maxPendingAcceptSessionCalls = value;
			}
		}

		public TimeSpan MessageWaitTimeout
		{
			get
			{
				return this.messageWaitTimeout;
			}
			set
			{
				Microsoft.ServiceBus.Common.TimeoutHelper.ThrowIfNegativeArgument(value, "value");
				this.messageWaitTimeout = value;
			}
		}

		internal bool WaitForPendingOperationsOnClose
		{
			get;
			set;
		}

		public SessionHandlerOptions()
		{
			this.AutoComplete = true;
			this.MaxConcurrentSessions = Environment.ProcessorCount * 1000;
			this.MessageWaitTimeout = TimeSpan.FromMinutes(1);
			this.AutoRenewTimeout = Constants.ClientPumpRenewLockTimeout;
			this.WaitForPendingOperationsOnClose = false;
		}

		internal void RaiseExceptionReceived(object source, ExceptionReceivedEventArgs args)
		{
			EventHandler<ExceptionReceivedEventArgs> eventHandler = this.ExceptionReceived;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(source, args);
				}
				catch (Exception exception)
				{
					throw Fx.AssertAndFailFastService(exception.ToString());
				}
			}
		}

		public event EventHandler<ExceptionReceivedEventArgs> ExceptionReceived;
	}
}