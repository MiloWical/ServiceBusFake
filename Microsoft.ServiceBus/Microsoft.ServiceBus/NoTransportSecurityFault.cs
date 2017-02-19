using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="NoTransportSecurityFault", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relay")]
	internal class NoTransportSecurityFault : IExtensibleDataObject
	{
		ExtensionDataObject System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
		{
			get;
			set;
		}

		public NoTransportSecurityFault()
		{
		}
	}
}