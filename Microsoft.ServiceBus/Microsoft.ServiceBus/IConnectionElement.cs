using Microsoft.ServiceBus.Channels;
using System;

namespace Microsoft.ServiceBus
{
	internal interface IConnectionElement
	{
		UriPrefixTable<ITransportManagerRegistration> TransportManagerTable
		{
			get;
		}

		IConnectionInitiator CreateInitiator(int bufferSize);

		IConnectionListener CreateListener(int bufferSize, Uri uri);

		T GetProperty<T>()
		where T : class;

		bool IsCompatible(IConnectionElement element);
	}
}