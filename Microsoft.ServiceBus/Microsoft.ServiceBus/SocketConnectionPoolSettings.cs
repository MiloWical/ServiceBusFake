using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;

namespace Microsoft.ServiceBus
{
	public sealed class SocketConnectionPoolSettings
	{
		private string groupName;

		private TimeSpan idleTimeout;

		private TimeSpan leaseTimeout;

		private int maxOutboundConnectionsPerEndpoint;

		public string GroupName
		{
			get
			{
				return this.groupName;
			}
			set
			{
				if (value == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
				}
				this.groupName = value;
			}
		}

		public TimeSpan IdleTimeout
		{
			get
			{
				return this.idleTimeout;
			}
			set
			{
				if (value < TimeSpan.Zero)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
				}
				if (TimeoutHelper.IsTooLarge(value))
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRangeTooBig, new object[0])));
				}
				this.idleTimeout = value;
			}
		}

		public TimeSpan LeaseTimeout
		{
			get
			{
				return this.leaseTimeout;
			}
			set
			{
				if (value < TimeSpan.Zero)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
				}
				if (TimeoutHelper.IsTooLarge(value))
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRangeTooBig, new object[0])));
				}
				this.leaseTimeout = value;
			}
		}

		public int MaxOutboundConnectionsPerEndpoint
		{
			get
			{
				return this.maxOutboundConnectionsPerEndpoint;
			}
			set
			{
				if (value < 0)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBeNonNegative, new object[0])));
				}
				this.maxOutboundConnectionsPerEndpoint = value;
			}
		}

		internal SocketConnectionPoolSettings()
		{
			this.groupName = "default";
			this.idleTimeout = ConnectionOrientedTransportDefaults.IdleTimeout;
			this.leaseTimeout = TcpTransportDefaults.ConnectionLeaseTimeout;
			this.maxOutboundConnectionsPerEndpoint = 10;
		}

		internal SocketConnectionPoolSettings(SocketConnectionPoolSettings tcp)
		{
			this.groupName = tcp.groupName;
			this.idleTimeout = tcp.idleTimeout;
			this.leaseTimeout = tcp.leaseTimeout;
			this.maxOutboundConnectionsPerEndpoint = tcp.maxOutboundConnectionsPerEndpoint;
		}

		internal SocketConnectionPoolSettings Clone()
		{
			return new SocketConnectionPoolSettings(this);
		}

		internal bool IsMatch(SocketConnectionPoolSettings tcp)
		{
			if (this.groupName != tcp.groupName)
			{
				return false;
			}
			if (this.idleTimeout != tcp.idleTimeout)
			{
				return false;
			}
			if (this.leaseTimeout != tcp.leaseTimeout)
			{
				return false;
			}
			if (this.maxOutboundConnectionsPerEndpoint != tcp.maxOutboundConnectionsPerEndpoint)
			{
				return false;
			}
			return true;
		}
	}
}