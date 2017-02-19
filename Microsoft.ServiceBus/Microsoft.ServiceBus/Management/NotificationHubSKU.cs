using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="NotificationHubSKU", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class NotificationHubSKU
	{
		[DataMember(Name="SKU", Order=1001)]
		[Key]
		public NotificationHubSKUType SKU;

		[DataMember(Name="MaxAllowedApiCallsPerDayPerUnit", EmitDefaultValue=false, Order=1008)]
		public long MaxAllowedApiCallsPerDayPerUnit
		{
			get;
			set;
		}

		[DataMember(Name="MaxAllowedDevicesPerUnit", Order=1006)]
		public long MaxAllowedDevicesPerUnit
		{
			get;
			set;
		}

		[DataMember(Name="MaxAllowedOperationsPerDayPerUnit", EmitDefaultValue=false, Order=1004)]
		public long MaxAllowedOperationsPerDayPerUnit
		{
			get;
			set;
		}

		[DataMember(Name="MaxAllowedPushesPerDayPerUnit", EmitDefaultValue=false, Order=1007)]
		public long MaxAllowedPushesPerDayPerUnit
		{
			get;
			set;
		}

		[DataMember(Name="MaxAllowedRegistrationsPerUnit", Order=1005)]
		public long MaxAllowedRegistrationsPerUnit
		{
			get;
			set;
		}

		[DataMember(Name="MaxAllowedUnits", Order=1002)]
		public int MaxAllowedUnits
		{
			get;
			set;
		}

		[DataMember(Name="MinAllowedUnits", Order=1003)]
		public int MinAllowedUnits
		{
			get;
			set;
		}

		public NotificationHubSKU()
		{
		}
	}
}