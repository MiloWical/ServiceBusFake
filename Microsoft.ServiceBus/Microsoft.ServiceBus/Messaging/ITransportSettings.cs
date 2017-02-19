using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface ITransportSettings
	{
		IAsyncResult BeginCreateFactory(IEnumerable<Uri> physicalUriAddresses, AsyncCallback callback, object state);

		object Clone();

		MessagingFactory EndCreateFactory(IAsyncResult result);
	}
}