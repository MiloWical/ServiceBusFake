using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="NamespaceSKU", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class MessagingSKU
	{
		[DataMember(Name="MaxAllowedEventHubUnit", Order=1004, IsRequired=false)]
		public int MaxAllowedEventHubunit
		{
			get;
			internal set;
		}

		[DataMember(Name="MinAllowedEventHubUnit", Order=1003, IsRequired=false)]
		public int MinAllowedEventHubUnit
		{
			get;
			internal set;
		}

		[DataMember(Name="SKU", Order=1001, IsRequired=true, EmitDefaultValue=true)]
		public int SKU
		{
			get;
			set;
		}

		[DataMember(Name="SKUDescription", Order=1002, IsRequired=false)]
		public string SKUDescription
		{
			get;
			internal set;
		}

		public MessagingSKU()
		{
		}
	}
}