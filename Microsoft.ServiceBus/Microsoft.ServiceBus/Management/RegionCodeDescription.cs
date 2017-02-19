using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="RegionCodeDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class RegionCodeDescription
	{
		public readonly static DataContractSerializer Serializer;

		[DataMember(Name="Code", IsRequired=true, Order=100, EmitDefaultValue=false)]
		public string Code
		{
			get;
			set;
		}

		[DataMember(Name="FullName", IsRequired=true, Order=101, EmitDefaultValue=false)]
		public string FullName
		{
			get;
			set;
		}

		static RegionCodeDescription()
		{
			RegionCodeDescription.Serializer = new DataContractSerializer(typeof(RegionCodeDescription));
		}

		public RegionCodeDescription()
		{
		}
	}
}