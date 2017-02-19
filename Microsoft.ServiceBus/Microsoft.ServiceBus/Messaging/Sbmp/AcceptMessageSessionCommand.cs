using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="AcceptMessageSession", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class AcceptMessageSessionCommand : IExtensibleDataObject
	{
		[DataMember(Name="sessionId", EmitDefaultValue=true, IsRequired=false, Order=65539)]
		private string sessionId;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65538)]
		private TimeSpan timeout;

		[DataMember(Name="prefetchCount", EmitDefaultValue=false, IsRequired=false, Order=65540)]
		private int prefetchMessageCount;

		[DataMember(Name="isSessionBrowser", IsRequired=false, EmitDefaultValue=false, Order=65541)]
		private bool isSessionBrowser;

		[DataMember(Name="operationTimeout", EmitDefaultValue=true, IsRequired=false, Order=65542)]
		private TimeSpan operationTimeout;

		[DataMember(Name="connectionNeutral", IsRequired=false, EmitDefaultValue=false, Order=65543)]
		private bool connectionNeutral;

		private ExtensionDataObject extensionData;

		public bool ConnectionNeutral
		{
			get
			{
				return this.connectionNeutral;
			}
			set
			{
				this.connectionNeutral = value;
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

		public bool IsSessionBrowser
		{
			get
			{
				return this.isSessionBrowser;
			}
			set
			{
				this.isSessionBrowser = value;
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

		public string SessionId
		{
			get
			{
				return this.sessionId;
			}
			set
			{
				this.sessionId = value;
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

		public AcceptMessageSessionCommand()
		{
		}
	}
}