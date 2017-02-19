using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[CollectionDataContract(Name="WnsHeaders", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect", ItemName="WnsHeader", KeyName="Header", ValueName="Value")]
	public sealed class WnsHeaderCollection : SortedDictionary<string, string>
	{
		public WnsHeaderCollection() : base(StringComparer.OrdinalIgnoreCase)
		{
		}
	}
}