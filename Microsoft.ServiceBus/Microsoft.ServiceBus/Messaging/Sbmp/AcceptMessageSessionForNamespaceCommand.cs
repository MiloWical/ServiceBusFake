using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="AcceptMessageSessionForNamespace", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class AcceptMessageSessionForNamespaceCommand : IExtensibleDataObject
	{
		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		private TimeSpan timeout;

		[DataMember(Name="prefetchCount", EmitDefaultValue=false, IsRequired=false, Order=65538)]
		private int prefetchMessageCount;

		[DataMember(Name="namespaceName", EmitDefaultValue=false, IsRequired=false, Order=65539)]
		private string namespaceName;

		[DataMember(Name="receiveMode", EmitDefaultValue=false, IsRequired=false, Order=65540)]
		private Microsoft.ServiceBus.Messaging.ReceiveMode receiveMode;

		[DataMember(Name="operationTimeout", EmitDefaultValue=true, IsRequired=false, Order=65542)]
		private TimeSpan operationTimeout;

		private ExtensionDataObject extensionData;

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

		public string Namespace
		{
			get
			{
				return this.namespaceName;
			}
			set
			{
				this.namespaceName = value;
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

		public int PrefetchCount
		{
			get
			{
				return this.prefetchMessageCount;
			}
			set
			{
				this.prefetchMessageCount = value;
			}
		}

		public Microsoft.ServiceBus.Messaging.ReceiveMode ReceiveMode
		{
			get
			{
				return this.receiveMode;
			}
			set
			{
				this.receiveMode = value;
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

		public AcceptMessageSessionForNamespaceCommand()
		{
		}
	}
}