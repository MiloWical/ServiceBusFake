using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="NamespaceSKUPlan", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class MessagingSKUPlan
	{
		[DataMember(Name="Revision", IsRequired=false, Order=1005, EmitDefaultValue=false)]
		public long Revision
		{
			get;
			set;
		}

		[DataMember(Name="SelectedEventHubUnit", Order=1002, IsRequired=false)]
		public int SelectedEventHubUnit
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

		[DataMember(Name="UpdatedAt", IsRequired=false, Order=1004, EmitDefaultValue=false)]
		public DateTime UpdatedAt
		{
			get;
			internal set;
		}

		public MessagingSKUPlan()
		{
		}
	}
}