using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="NamespaceAvailability", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class NamespaceAvailability : IExtensibleDataObject
	{
		public readonly static DataContractSerializer Serializer;

		[DataMember(Name="Result", Order=101)]
		public bool Available
		{
			get;
			set;
		}

		public ExtensionDataObject ExtensionData
		{
			get;
			set;
		}

		public UnavailableReason Reason
		{
			get;
			set;
		}

		[DataMember(Name="ReasonDetail", Order=102)]
		public string ReasonDetail
		{
			get;
			set;
		}

		static NamespaceAvailability()
		{
			NamespaceAvailability.Serializer = new DataContractSerializer(typeof(NamespaceAvailability));
		}

		public NamespaceAvailability()
		{
		}
	}
}