using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="EndpointNotFoundFault", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relay")]
	internal class EndpointNotFoundFault : IExtensibleDataObject
	{
		ExtensionDataObject System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
		{
			get;
			set;
		}

		public EndpointNotFoundFault()
		{
		}
	}
}