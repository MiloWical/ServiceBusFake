using Microsoft.ServiceBus.Channels;
using System;

namespace Microsoft.ServiceBus
{
	internal interface IRelayedConnectionParent
	{
		void Failure(object sender, Exception exception);

		void Success(object sender, IConnection socket);
	}
}