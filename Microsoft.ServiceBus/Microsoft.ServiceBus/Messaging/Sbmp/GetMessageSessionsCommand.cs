using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="GetMessageSessions", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class GetMessageSessionsCommand : IExtensibleDataObject
	{
		[DataMember(Name="skip", EmitDefaultValue=false, IsRequired=false, Order=65537)]
		private int skip;

		[DataMember(Name="top", EmitDefaultValue=false, IsRequired=false, Order=65538)]
		private int top;

		[DataMember(Name="lastUpdatedTime", EmitDefaultValue=true, IsRequired=false, Order=65539)]
		private DateTime lastUpdatedTime;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65540)]
		private TimeSpan timeout;

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

		public DateTime LastUpdatedTime
		{
			get
			{
				return this.lastUpdatedTime;
			}
			set
			{
				this.lastUpdatedTime = value;
			}
		}

		public int Skip
		{
			get
			{
				return this.skip;
			}
			set
			{
				this.skip = value;
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

		public int Top
		{
			get
			{
				return this.top;
			}
			set
			{
				this.top = value;
			}
		}

		public GetMessageSessionsCommand()
		{
		}
	}
}