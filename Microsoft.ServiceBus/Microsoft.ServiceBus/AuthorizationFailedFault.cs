using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="AuthorizationFailedFault", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relay")]
	internal class AuthorizationFailedFault : IExtensibleDataObject
	{
		ExtensionDataObject System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
		{
			get;
			set;
		}

		public AuthorizationFailedFault()
		{
		}
	}
}