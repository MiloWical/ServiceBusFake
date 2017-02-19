using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="TryReceive", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class TryReceiveCommand : IExtensibleDataObject
	{
		[DataMember(Name="messageCount", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		private int messageCount;

		[DataMember(Name="appMessageIds", EmitDefaultValue=true, IsRequired=false, Order=65538)]
		private IEnumerable<long> appMessageIds;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65539)]
		private TimeSpan timeout;

		[DataMember(Name="messageVersion", EmitDefaultValue=false, IsRequired=false, Order=131073)]
		private int messageVersion;

		[DataMember(Name="operationTimeout", EmitDefaultValue=true, IsRequired=false, Order=131074)]
		private TimeSpan operationTimeout;

		private ExtensionDataObject extensionData;

		public IEnumerable<long> AppMessageIds
		{
			get
			{
				return this.appMessageIds;
			}
			set
			{
				this.appMessageIds = value;
			}
		}

		public ExtensionDataObject ExtensionData
		{
			get
			{
				return this.extensionData;
			}
			set
			{
				this.extensionData = value;
			}
		}

		public int MessageCount
		{
			get
			{
				return this.messageCount;
			}
			set
			{
				this.messageCount = value;
			}
		}

		public int MessageVersion
		{
			get
			{
				if (this.messageVersion == 0)
				{
					return BrokeredMessage.MessageVersion1;
				}
				return this.messageVersion;
			}
			set
			{
				this.messageVersion = value;
			}
		}

		public TimeSpan OperationTimeout
		{
			get
			{
				if (this.operationTimeout != TimeSpan.Zero)
				{
					return this.operationTimeout;
				}
				return this.Timeout;
			}
			set
			{
				this.operationTimeout = value;
			}
		}

		public TimeSpan Timeout
		{
			get
			{
				return this.timeout;
			}
			set
			{
				this.timeout = value;
			}
		}

		public TryReceiveCommand()
		{
		}
	}
}