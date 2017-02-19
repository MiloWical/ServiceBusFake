using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal static class EndpointAddress2
	{
		private static Uri anonymousUri;

		private static EndpointAddress anonymousAddress;

		internal static EndpointAddress AnonymousAddress
		{
			get
			{
				if (EndpointAddress2.anonymousAddress == null)
				{
					EndpointAddress2.anonymousAddress = new EndpointAddress(EndpointAddress2.AnonymousUri, new AddressHeader[0]);
				}
				return EndpointAddress2.anonymousAddress;
			}
		}

		public static Uri AnonymousUri
		{
			get
			{
				if (EndpointAddress2.anonymousUri == null)
				{
					EndpointAddress2.anonymousUri = new Uri("http://schemas.microsoft.com/2005/12/ServiceModel/Addressing/Anonymous");
				}
				return EndpointAddress2.anonymousUri;
			}
		}
	}
}