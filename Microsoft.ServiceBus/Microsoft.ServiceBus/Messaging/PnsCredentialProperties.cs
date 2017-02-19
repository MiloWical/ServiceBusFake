using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[CollectionDataContract(Name="Properties", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect", ItemName="Property", KeyName="Name", ValueName="Value")]
	public sealed class PnsCredentialProperties : Dictionary<string, string>
	{
		public PnsCredentialProperties() : base(StringComparer.OrdinalIgnoreCase)
		{
		}
	}
}