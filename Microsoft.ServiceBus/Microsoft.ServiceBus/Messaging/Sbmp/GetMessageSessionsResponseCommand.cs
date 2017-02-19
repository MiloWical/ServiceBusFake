using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="GetMessageSessionsResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class GetMessageSessionsResponseCommand : IExtensibleDataObject
	{
		[DataMember(Name="sessions", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		private IEnumerable<Tuple<string, SessionState>> sessions;

		[DataMember(Name="skip", EmitDefaultValue=false, IsRequired=false, Order=65538)]
		private int skip;

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

		public IEnumerable<Tuple<string, SessionState>> Sessions
		{
			get
			{
				return this.sessions;
			}
			set
			{
				this.sessions = value;
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

		public GetMessageSessionsResponseCommand()
		{
		}
	}
}