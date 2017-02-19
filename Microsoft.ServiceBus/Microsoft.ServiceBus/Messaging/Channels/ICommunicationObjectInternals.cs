using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal interface ICommunicationObjectInternals : ICommunicationObject
	{
		void ThrowIfDisposed();

		void ThrowIfDisposedOrNotOpen();
	}
}