using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[CollectionDataContract(Name="MpnsHeaders", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect", ItemName="MpnsHeader", KeyName="Header", ValueName="Value")]
	public sealed class MpnsHeaderCollection : SortedDictionary<string, string>
	{
		public MpnsHeaderCollection() : base(StringComparer.OrdinalIgnoreCase)
		{
		}
	}
}