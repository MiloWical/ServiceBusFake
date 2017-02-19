using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus
{
	public sealed class NamespaceManagerSettings
	{
		private int getEntitiesPageSize;

		private TimeSpan operationTimeout;

		private Microsoft.ServiceBus.RetryPolicy retryPolicy;

		internal Microsoft.ServiceBus.Messaging.FaultInjectionInfo FaultInjectionInfo
		{
			get;
			set;
		}

		internal int GetEntitiesPageSize
		{
			get
			{
				return this.getEntitiesPageSize;
			}
			set
			{
				if (value <= 0)
				{
					throw FxTrace.Exception.ArgumentOutOfRange("GetEntitiesPageSize", value, "GetEntitiesPageSize has to be positive value");
				}
				this.getEntitiesPageSize = value;
			}
		}

		internal TimeSpan InternalOperationTimeout
		{
			get
			{
				if (this.operationTimeout > Constants.MaxOperationTimeout)
				{
					return Constants.MaxOperationTimeout;
				}
				return this.operationTimeout;
			}
		}

		public TimeSpan OperationTimeout
		{
			get
			{
				return this.operationTimeout;
			}
			set
			{
				TimeoutHelper.ThrowIfNonPositiveArgument(value, "OperationTimeout");
				this.operationTimeout = value;
			}
		}

		public Microsoft.ServiceBus.RetryPolicy RetryPolicy
		{
			get
			{
				return this.retryPolicy;
			}
			set
			{
				if (value == null)
				{
					throw FxTrace.Exception.ArgumentNull("RetryPolicy");
				}
				this.retryPolicy = value;
			}
		}

		public Microsoft.ServiceBus.TokenProvider TokenProvider
		{
			get;
			set;
		}

		public NamespaceManagerSettings()
		{
			this.operationTimeout = Constants.DefaultOperationTimeout;
			this.retryPolicy = Microsoft.ServiceBus.RetryPolicy.Default;
			this.getEntitiesPageSize = 2147483647;
			this.TokenProvider = null;
		}
	}
}