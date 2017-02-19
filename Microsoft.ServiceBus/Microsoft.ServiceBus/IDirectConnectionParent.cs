using System;
using System.Net.Sockets;

namespace Microsoft.ServiceBus
{
	internal interface IDirectConnectionParent
	{
		void Failure(object sender, Exception exception);

		void Success(object sender, Socket socket);
	}
}